// dllmain.cpp : Defines the entry point for the DLL application.
// Put this before any Windows headers
#define NOMINMAX
#include <windows.h>
#include "pch.h"
#include <opencv2/opencv.hpp>
#include <time.h>
#include <iostream>
#include <algorithm>
#include <fstream>
#include <filesystem>
#include <iomanip>
#include <sstream>
#include <stdexcept>
#include <string>
#include <vector>

namespace fs = std::filesystem;

constexpr double PixToMm = 0.008951113022; //pixel per MM
constexpr double kOffsetMm = 0.004808987656; // the offset of the camera in MM


struct Measurement {
	double xPin;		//horizontal position of the pin.
	int yPin;			//vertical position of the pn.
	int yFloor;			//vertical position of the floor.
	double gapPx;		//the gap between the pin and the floor in pixels.
	double distanceMm;	//the distance between the pin and the floor in MM.
};

cv::Mat imread_unicode(const char* filename, int flags = cv::IMREAD_COLOR);
static std::vector<double> numpyGradient(const std::vector<double>& values);
static int strongestGradient(const std::vector<double>& gradient,
	const std::vector<double>& profile,
	int begin, int end);
static int firstGradientForFloor(const std::vector<double>& gradient,
	int begin, int end);
static int detectYPin(const cv::Mat& srcGray);
Measurement detectMeasurement(const cv::Mat& image);

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

#define CHECK_EXPIRED 0
#if cpp_call
extern "C" __declspec(dllexport) int Recognition(cv::String imageName, int mode, int* dimensions)
#else
struct MyData {
	double dimensions[5];
};

extern "C" __declspec(dllexport) int Recognition(char* imageName, int mode, MyData* pData)
#endif
{
	try
	{
#if CHECK_EXPIRED
		bool expired = false;
		SYSTEMTIME sys;
		GetLocalTime(&sys);
		if (sys.wYear > exp_year) expired = true;
		else if (sys.wYear == exp_year) {
			if (sys.wMonth > exp_month) expired = true;
			else if (sys.wMonth == exp_month) {
				if (sys.wDay > exp_day) expired = true;
			}
		}
		if (expired) return -1;
#endif
		cv::Mat frame = imread_unicode(imageName), img;
		if (frame.cols < 1 || frame.rows < 1) return 2;
		cv::Mat gray;
		cv::cvtColor(frame, gray, cv::COLOR_BGR2GRAY);
		
		img = frame;

		const Measurement measurement = detectMeasurement(gray);
		pData->dimensions[0] = measurement.xPin;
		pData->dimensions[1] = measurement.yPin;
		pData->dimensions[2] = measurement.yFloor;
		pData->dimensions[3] = measurement.gapPx;
		pData->dimensions[4] = measurement.distanceMm;
		

		int lineLength = 20;
		int thickness = 1;
		
		cv::Point p1(pData->dimensions[0] - lineLength / 2, pData->dimensions[1]);
		cv::Point p2(pData->dimensions[0] + lineLength / 2, pData->dimensions[1]);
		cv::Point p3(pData->dimensions[0] - lineLength / 2, pData->dimensions[2]);
		cv::Point p4(pData->dimensions[0] + lineLength / 2, pData->dimensions[2]);
		cv::Point p5(pData->dimensions[0], pData->dimensions[1]);
		cv::Point p6(pData->dimensions[0], pData->dimensions[2]);



		cv::Scalar redBGR(0, 0, 255);
		cv::line(frame, p1, p2, redBGR, thickness, cv::LINE_AA);
		cv::line(frame, p3, p4, redBGR, thickness, cv::LINE_AA);
		cv::line(frame, p5, p6, redBGR, thickness, cv::LINE_AA);
		//cv::imshow("111", img);
		//cv::waitKey(0);

		if (mode < 3) { // example: mode==1 => output grayscale
			cv::Mat gray1;
			cv::cvtColor(img, gray1, cv::COLOR_BGR2GRAY);
			if (!cv::imwrite("output1.png", frame)) return -4;

			// fill dimensions as width/height
			//if (pData) { pData->dimensions[0] = gray1.cols; pData->dimensions[1] = gray1.rows; }
		}
		else {
			// save color image with red line
			if (!cv::imwrite("output1.png", img)) return -5;
			if (pData) { pData->dimensions[0] = img.cols; pData->dimensions[1] = img.rows; }
		}
	}
	catch (...)
	{
		return 1;
	}
	return 0;
}


cv::Mat imread_unicode(const char* filename, int flags) {
	// 1️⃣ Convert UTF-8 char* to wstring
	int len = MultiByteToWideChar(CP_UTF8, 0, filename, -1, NULL, 0);
	std::wstring wfilename(len, L'\0');
	MultiByteToWideChar(CP_UTF8, 0, filename, -1, &wfilename[0], len);

	// 2️⃣ Open file with _wifstream (supports wstring path on Windows)
	std::wifstream file(wfilename, std::ios::binary);
	if (!file) return cv::Mat();  // failed to open

	// 3️⃣ Read file into buffer
	std::vector<char> buffer((std::istreambuf_iterator<wchar_t>(file)), {});

	// ❌ Above won't work because _wifstream reads wchar_t
	// Instead, use Windows API ReadFile for binary data
	HANDLE hFile = CreateFileW(wfilename.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile == INVALID_HANDLE_VALUE) return cv::Mat();

	DWORD fileSize = GetFileSize(hFile, NULL);
	std::vector<BYTE> buf(fileSize);
	DWORD bytesRead = 0;
	if (!ReadFile(hFile, buf.data(), fileSize, &bytesRead, NULL)) {
		CloseHandle(hFile);
		return cv::Mat();
	}
	CloseHandle(hFile);

	// 4️⃣ Decode image from memory
	return cv::imdecode(buf, flags);
}


//This function calculates the gradient of a vector of values
//It uses central differences for interior points and forward/backward 
// differences for the endpoints.

static std::vector<double> numpyGradient(const std::vector<double>& values) {
	if (values.size() < 2) {
		throw std::runtime_error("Profile is too short for gradient calculation.");
	}

	std::vector<double> gradient(values.size());
	gradient.front() = values[1] - values[0];
	gradient.back() = values.back() - values[values.size() - 2];

	for (size_t i = 1; i + 1 < values.size(); ++i) {
		gradient[i] = (values[i + 1] - values[i - 1]) / 2.0;
	}

	return gradient;
}

Measurement detectMeasurement(const cv::Mat& image) {
	//std::string type = image.type();
	int t = image.type();
	if (image.empty()) {
		throw std::runtime_error("Expected a non-empty grayscale image.");
	}

	cv::Mat blur;
	cv::GaussianBlur(image, blur, cv::Size(5, 5), 0); //delete the noise in the image

	const int upperRows = std::min(220, (blur.rows)/2);
	cv::Mat upper = blur.rowRange(0, upperRows);// upper is the upper part of the image, which is used to detect the pin (0~160)
	/*cv::imshow("upper", upper);
	cv::waitKey(0)*/;
	cv::Mat mask;
	cv::compare(upper, 80, mask, cv::CMP_LT);//mask is a binary image, which is used to detect the pin (0~160)
	cv::Mat labels, stats, centroids;
	const int componentCount = cv::connectedComponentsWithStats(
		mask, labels, stats, centroids, 8, CV_32S);
	
	double bestArea = -1.0;
	double xPin = 0.0;
	for (int i = 1; i < componentCount; ++i) {
		const int y = stats.at<int>(i, cv::CC_STAT_TOP);//top coordinate of the bounding box i
		const int height = stats.at<int>(i, cv::CC_STAT_HEIGHT);//
		const int area = stats.at<int>(i, cv::CC_STAT_AREA);
		if (y <20 && height >= 30 && area >= 100 && area > bestArea) {
			bestArea = area;
			xPin = centroids.at<double>(i, 0);//9.7  10.am. I think xPin value is wrong. Change this 
		}
	}
	if (bestArea < 0.0) {
		throw std::runtime_error("Pin not detected.");
	}

	const int xc = static_cast<int>(std::lround(xPin));
	constexpr int halfBand =40;
	const int x0 = std::max(0, xc - halfBand);
	const int x1 = std::min(image.cols, xc + halfBand + 1);
	if (x0 >= x1) {
		throw std::runtime_error("Pin profile band is outside the image.");
	}

	std::vector<double> profile(static_cast<size_t>(blur.rows), 0.0);
	for (int y = 0; y < blur.rows; ++y) {
 		profile[static_cast<size_t>(y)] =
			cv::mean(blur.row(y).colRange(x0, x1))[0];
	}
	const std::vector<double> gradient = numpyGradient(profile);
	if (blur.rows < 490) {
		throw std::runtime_error("Image is shorter than the expected search regions.");
	}

 //	auto strongestGradient = [&gradient](int begin, int end) {
	//	int best = begin;
	//	double bestValue = -1.0;
	//	bool endDropletFlag = false;
	//	for (int i = begin; i < end; ++i) {
	//		const double value = std::abs(gradient[static_cast<size_t>(i)]);
	//		if (value > bestValue && (i+10)<end) {
	//			/*for (int j = 0; j < 10; j++) {
	//				if (profile[static_cast<size_t>(i+j)] > 240)
	//					endDropletFlag = true;
	//			}*/
	//			if (endDropletFlag) {
	//				bestValue = value;
	//				best = i;
	//			}
	//			
	//		}
	//	}
	//	return best;
	//};  
	//auto firstGradientForFloor = [&gradient](int begin, int end) {
	//	int floor = begin;
	//	double thereshold = 1.0;
	//	for (int i = begin; i < end; ++i) {
	//		const double value = std::abs(gradient[static_cast<size_t>(i)]);
	//		if (value > thereshold) {
	//			thereshold = value;
	//			floor = i;
	//			break;
	//		}
	//	}
	//	return floor;
	//};

	const int yPin = strongestGradient(gradient, profile, 20, (blur.rows)*2/3 + 20);
	//const int yPin = detectYPin(blur);
	const int yFloor = firstGradientForFloor(gradient, yPin+10, blur.rows);
	const double gapPx = static_cast<double>(yFloor - yPin);
	return { xPin, yPin, yFloor, gapPx, PixToMm * gapPx + kOffsetMm};
}
static int strongestGradient(const std::vector<double>& gradient,
	const std::vector<double>& profile,
	int begin, int end)
{
	int best = begin;
	double bestValue = -1.0;

	for (int i = begin; i < end; ++i) {
		size_t idx = static_cast<size_t>(i);
		if (idx >= gradient.size()) break;

		double value = std::abs(gradient[idx]);

		// require room for the profile check
		if (value > bestValue && (i + 10) < end) {
			bool endDropletFlag = true;

			// check next 10 profile samples (bounds-checked)
			for (int j = 0; j < 10; ++j) {
				size_t pidx = idx + static_cast<size_t>(j);
				if (pidx >= profile.size() || profile[pidx] <= 240) {
					endDropletFlag = false;
					break;
				}
			}

			if (endDropletFlag) {
				bestValue = value;
				best = i;
			}
		}
	}

	return best;
}

static int firstGradientForFloor(const std::vector<double>& gradient,
	int begin, int end)
{
	int floorIdx = begin;
	double threshold = 1.0;

	for (int i = begin; i < end; ++i) {
		size_t idx = static_cast<size_t>(i);
		if (idx >= gradient.size()) break;

		double value = std::abs(gradient[idx]);
		if (value > threshold) {
			threshold = value;
			floorIdx = i;
			break;
		}
	}
	if (floorIdx == begin)
	{
		floorIdx = end;
	}

	return floorIdx;
}

// Detect dark vertical pin region and return y coordinate (bottom of bounding box).
// srcGray must be CV_8U grayscale.
static int detectYPin(const cv::Mat& srcGray)
{
	if (srcGray.empty() || srcGray.type() != CV_8U) return -1;

	// Preprocess: blur to remove noise
	cv::Mat blur;
	cv::GaussianBlur(srcGray, blur, cv::Size(5, 5), 1.5);

	// Use Otsu to separate dark foreground. We invert so dark objects become white.
	cv::Mat bin;
	cv::threshold(blur, bin, 0, 255, cv::THRESH_BINARY_INV | cv::THRESH_OTSU);

	// Morphology to remove small noise and close the pin
	cv::Mat kernel = cv::getStructuringElement(cv::MORPH_RECT, cv::Size(5, 7));
	cv::morphologyEx(bin, bin, cv::MORPH_CLOSE, kernel);
	cv::morphologyEx(bin, bin, cv::MORPH_OPEN, kernel);

	// Find contours
	std::vector<std::vector<cv::Point>> contours;
	cv::findContours(bin, contours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

	if (contours.empty()) return -1;

	// Select the best candidate: tall, large area, and near top region
	int bestY = -1;
	double bestScore = 0.0;
	const int rows = srcGray.rows;
	const int candidateTopLimit = std::max(1, rows / 3); // prefer contours whose top is in top third

	for (auto& cnt : contours) {
		cv::Rect r = cv::boundingRect(cnt);
		double area = static_cast<double>(r.width) * r.height;
		if (area < 50.0) continue; // ignore tiny blobs (tune if necessary)

		double aspect = (r.height > 0) ? (static_cast<double>(r.height) / r.width) : 0.0;
		// score: prefer tall shapes near top and with larger area
		double score = area * (1.0 + std::max(0.0, aspect - 2.0)); // favor aspect > 2

		// boost if the top is close to the image top (pin hangs from top)
		if (r.y < candidateTopLimit) score *= 1.5;

		if (score > bestScore) {
			bestScore = score;
			bestY = r.y + r.height - 1; // bottom y of the rectangle
		}
	}

	// Optional: debug visual output
	if (bestY >= 0) {
		cv::Mat dbg;
		cv::cvtColor(srcGray, dbg, cv::COLOR_GRAY2BGR);
		for (auto& cnt : contours) {
			cv::Rect r = cv::boundingRect(cnt);
			cv::rectangle(dbg, r, cv::Scalar(0, 255, 0), 1);
		}
		// draw detected bottom line
		cv::line(dbg, cv::Point(0, bestY), cv::Point(dbg.cols - 1, bestY), cv::Scalar(0, 0, 255), 2);
		cv::imwrite("debug_pin.png", dbg);
	}

	return bestY;
}