// faceRecoTest.cpp : �������̨Ӧ�ó������ڵ㡣
//

#include "stdafx.h"
#include <stdlib.h>
#include "arcsoft_face_sdk.h"
#include "amcomdef.h"
#include "asvloffscreen.h"
#include "merror.h"
#include <direct.h>
#include <iostream>  
#include <stdarg.h>
#include <string>
#include <opencv2\opencv.hpp>

using namespace std;
#pragma comment(lib, "libarcsoft_face_engine.lib")

#define APPID "FjH7hTxgMzSZ2tfqZqCpoEXw3axPHUZKZXg9zmfVSAJy"
#define SDKKey "4F37jPqp8hfu227Y2XLpYjX6GgsobvHtw551bShs6ycw"

#define SafeFree(p) { if ((p)) free(p); (p) = NULL; }
#define SafeArrayDelete(p) { if ((p)) delete [] (p); (p) = NULL; } 
#define SafeDelete(p) { if ((p)) delete (p); (p) = NULL; } 

//�ü�ͼƬ
void CutIplImage(IplImage* src, IplImage* dst, int x, int y)
{
	CvSize size = cvSize(dst->width, dst->height);//�����С
	cvSetImageROI(src, cvRect(x, y, size.width, size.height));//����Դͼ��ROI
	cvCopy(src, dst); //����ͼ��
	cvResetImageROI(src);//Դͼ����������ROI
}

int __stdcall main()
{
	//����ӿ�
	MRESULT res = ASFActivation(APPID, SDKKey);
	if (MOK != res && MERR_ASF_ALREADY_ACTIVATED != res)
		printf("ASFActivation fail: %d\n", res);
	else
		printf("ASFActivation sucess: %d\n", res);

	//��ʼ���ӿ�
	MHandle handle = NULL;
	MInt32 mask = ASF_FACE_DETECT | ASF_FACERECOGNITION | ASF_AGE | ASF_GENDER | ASF_FACE3DANGLE | ASF_LIVENESS;
	res = ASFInitEngine(ASF_DETECT_MODE_IMAGE, ASF_OP_0_ONLY, 16, 10, mask, &handle);
	if (res != MOK)
		printf("ASFInitEngine fail: %d\n", res);
	else
		printf("ASFInitEngine sucess: %d\n", res);

	// �������
	IplImage* img0 = cvLoadImage("C:\\congcong.png");
	IplImage* img1 = cvLoadImage("C:\\pupu.jpg");

	if (img0 && img1)
	{
		ASF_MultiFaceInfo detectedFaces1 = { 0 };
		ASF_SingleFaceInfo SingleDetectedFaces1 = { 0 };
		ASF_FaceFeature feature1 = { 0 };
		ASF_FaceFeature copyfeature1 = { 0 };
		IplImage* cutImg0 = cvCreateImage(cvSize(img0->width - img0->width % 4, img0->height), IPL_DEPTH_8U, img0->nChannels);
		CutIplImage(img0, cutImg0, 0, 0); ``
			res = ASFDetectFaces(handle, cutImg0->width, cutImg0->height, ASVL_PAF_RGB24_B8G8R8, (MUInt8*)cutImg0->imageData, &detectedFaces1);
		if (MOK == res)
		{
			SingleDetectedFaces1.faceRect.left = detectedFaces1.faceRect[0].left;
			SingleDetectedFaces1.faceRect.top = detectedFaces1.faceRect[0].top;
			SingleDetectedFaces1.faceRect.right = detectedFaces1.faceRect[0].right;
			SingleDetectedFaces1.faceRect.bottom = detectedFaces1.faceRect[0].bottom;
			SingleDetectedFaces1.faceOrient = detectedFaces1.faceOrient[0];

			res = ASFFaceFeatureExtract(handle, cutImg0->width, cutImg0->height, ASVL_PAF_RGB24_B8G8R8, (MUInt8*)cutImg0->imageData, &SingleDetectedFaces1, &feature1);
			if (res == MOK)
			{
				//����feature
				copyfeature1.featureSize = feature1.featureSize;
				copyfeature1.feature = (MByte *)malloc(feature1.featureSize);
				memset(copyfeature1.feature, 0, feature1.featureSize);
				memcpy(copyfeature1.feature, feature1.feature, feature1.featureSize);
			}
			else
				printf("ASFFaceFeatureExtract 1 fail: %d\n", res);
		}
		else
			printf("ASFDetectFaces 1 fail: %d\n", res);



		//�ڶ���������ȡ����
		ASF_MultiFaceInfo	detectedFaces2 = { 0 };
		ASF_SingleFaceInfo SingleDetectedFaces2 = { 0 };
		ASF_FaceFeature feature2 = { 0 };
		IplImage* cutImg1 = cvCreateImage(cvSize(img1->width - img1->width % 4, img1->height), IPL_DEPTH_8U, img1->nChannels);
		CutIplImage(img1, cutImg1, 0, 0);
		res = ASFDetectFaces(handle, cutImg1->width, cutImg1->height, ASVL_PAF_RGB24_B8G8R8, (MUInt8*)cutImg1->imageData, &detectedFaces2);
		if (MOK == res)
		{
			SingleDetectedFaces2.faceRect.left = detectedFaces2.faceRect[0].left;
			SingleDetectedFaces2.faceRect.top = detectedFaces2.faceRect[0].top;
			SingleDetectedFaces2.faceRect.right = detectedFaces2.faceRect[0].right;
			SingleDetectedFaces2.faceRect.bottom = detectedFaces2.faceRect[0].bottom;
			SingleDetectedFaces2.faceOrient = detectedFaces2.faceOrient[0];

			res = ASFFaceFeatureExtract(handle, cutImg1->width, cutImg1->height, ASVL_PAF_RGB24_B8G8R8, (MUInt8*)cutImg1->imageData, &SingleDetectedFaces2, &feature2);
			if (MOK != res)
				printf("ASFFaceFeatureExtract 2 fail: %d\n", res);
		}
		else
			printf("ASFDetectFaces 2 fail: %d\n", res);


		// �����������ȶ�
		MFloat confidenceLevel;
		res = ASFFaceFeatureCompare(handle, &copyfeature1, &feature2, &confidenceLevel);
		if (res != MOK)
			printf("ASFFaceFeatureCompare fail: %d\n", res);
		else
			printf("ASFFaceFeatureCompare sucess: %lf\n", confidenceLevel);

		// ������Ϣ���
		MInt32 processMask = ASF_AGE | ASF_GENDER | ASF_FACE3DANGLE | ASF_LIVENESS;
		res = ASFProcess(handle, cutImg1->width, cutImg1->height, ASVL_PAF_RGB24_B8G8R8, (MUInt8*)cutImg1->imageData, &detectedFaces1, processMask);
		if (res != MOK)
			printf("ASFProcess fail: %d\n", res);
		else
			printf("ASFProcess sucess: %d\n", res);

		// ��ȡ����
		ASF_AgeInfo ageInfo = { 0 };
		res = ASFGetAge(handle, &ageInfo);
		if (res != MOK)
			printf("ASFGetAge fail: %d\n", res);
		else
			printf("ASFGetAge sucess: %d\n", res);

		// ��ȡ�Ա�
		ASF_GenderInfo genderInfo = { 0 };
		res = ASFGetGender(handle, &genderInfo);
		if (res != MOK)
			printf("ASFGetGender fail: %d\n", res);
		else
			printf("ASFGetGender sucess: %d\n", res);

		// ��ȡ3D�Ƕ�
		ASF_Face3DAngle angleInfo = { 0 };
		res = ASFGetFace3DAngle(handle, &angleInfo);
		if (res != MOK)
			printf("ASFGetFace3DAngle fail: %d\n", res);
		else
			printf("ASFGetFace3DAngle sucess: %d\n", res);

		//��ȡ������Ϣ
		ASF_LivenessInfo livenessInfo = { 0 };
		res = ASFGetLivenessScore(handle, &livenessInfo);
		if (res != MOK)
			printf("ASFGetLivenessScore fail: %d\n", res);
		else
			printf("ASFGetLivenessScore sucess: %d\n", livenessInfo.isLive[0]);

		SafeFree(copyfeature1.feature);		//�ͷ��ڴ�
		cvReleaseImage(&cutImg0);
		cvReleaseImage(&cutImg1);
	}
	cvReleaseImage(&img0);
	cvReleaseImage(&img1);
	//��ȡ�汾��Ϣ
	const ASF_VERSION* pVersionInfo = ASFGetVersion(handle);

	//����ʼ��
	res = ASFUninitEngine(handle);
	if (res != MOK)
		printf("ALUninitEngine fail: %d\n", res);
	else
		printf("ALUninitEngine sucess: %d\n", res);

	getchar();
	return 0;
}

