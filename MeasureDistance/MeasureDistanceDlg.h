
// MeasureDistanceDlg.h : header file
//

#pragma once
#include "afxwin.h"
#include <thread>
#include <atomic>

#define WM_IMAGE_CLICKED (WM_APP + 100)

class CClickableStatic : public CStatic
{
public:
	CClickableStatic() : m_hasPoint(false) {}
	void SetClickPoint(const CPoint& pt) { m_clickPoint = pt; m_hasPoint = true; Invalidate(); }
	void ClearClickPoint() { m_hasPoint = false; Invalidate(); }

protected:
	afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
	afx_msg void OnPaint();
	DECLARE_MESSAGE_MAP()

private:
	CPoint m_clickPoint;
	bool m_hasPoint;
};

// CMeasureDistanceDlg dialog
class CMeasureDistanceDlg : public CDialogEx
{
// Construction
public:
	CMeasureDistanceDlg(CWnd* pParent = nullptr);	// standard constructor

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_MEASUREDISTANCE_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation


protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()


public:
	CMenu m_menu;

private:
	CListCtrl m_image_list;
	int m_mode = 0;
	CClickableStatic m_pic_image;      // use subclass here
	//CStatic m_pic_image;
	HBITMAP m_hBitmapPic = nullptr; // currently displayed bitmap (must be freed)
	CString dstFolder, imageName;
	CDC* pImgDC = nullptr;
	CRect orgRect;
	int drawWidth = 0;
	int drawHeight = 0;
	bool imageLoaded;
	CImage img;
	CString preTitle;

	// semiauto click state (image-coordinates or control coords as you prefer)
	CPoint m_semiClickCtrl; // control coords of last click
	bool m_hasSemiClick = false;

public:
	afx_msg void OnBnClickedRadioAuto();
	afx_msg void OnBnClickedRadioSemiauto();
	afx_msg void OnBnClickedRadioManual();
	afx_msg void OnMenuOpenFolder();
	afx_msg void OnBnClickedBtnStop();
	afx_msg LRESULT OnImageClicked(WPARAM wParam, LPARAM lParam);

public:
	afx_msg void OnBnClickedButtonRecog();
	
	// posted from worker thread to update UI for each processed item:
	afx_msg LRESULT OnRecogItemProcessed(WPARAM wParam, LPARAM lParam);
	// posted when worker finished
	afx_msg LRESULT OnRecogFinished(WPARAM wParam, LPARAM lParam);
	// add declaration in the public/afx_msg section
	afx_msg void OnLvnItemchangedListImageName(NMHDR* pNMHDR, LRESULT* pResult);

private:
	int m_currentIndex = -1;
	void DoRecog(int index = -1);
	void loadImage();
	void OnDestroy();


	// background worker
	std::thread m_worker;
	std::atomic<bool> m_processing{ false };
	std::atomic<bool> m_cancel{ false };
	
};
