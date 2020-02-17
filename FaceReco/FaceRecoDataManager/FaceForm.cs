﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using YunZhiFaceReco.Entity;
using YunZhiFaceReco.SDKModels;
using YunZhiFaceReco.SDKUtil;
using YunZhiFaceReco.TV_Create.MUti_Channel;
using YunZhiFaceReco.TV_Create.MUti_Channel.pojo;
using YunZhiFaceReco.Utils;

namespace YunZhiFaceReco {
    public partial class FaceForm : Form {
        //引擎Handle
        private IntPtr pImageEngine = IntPtr.Zero;

        //保存右侧图片路径
        private string image1Path;

        //右侧图片人脸特征
        private IntPtr image1Feature;

        //保存对比图片的列表
        private List<string> imagePathList = new List<string>();

        //左侧图库人脸特征列表
        private List<IntPtr> imagesFeatureList = new List<IntPtr>();

        //相似度
        private float threshold = 0.8f;

        //用于标记是否需要清除比对结果
        private bool isCompare = false;

        #region 视频模式下相关

        //视频引擎Handle
        private IntPtr pVideoEngine = IntPtr.Zero;

        //视频引擎 FR Handle 处理   FR和图片引擎分开，减少强占引擎的问题
        private IntPtr pVideoImageEngine = IntPtr.Zero;
        /// <summary>
        /// 视频输入设备信息
        /// </summary>
        private FilterInfoCollection filterInfoCollection;
        private VideoCaptureDevice deviceVideo;

        #endregion

        public FaceForm() {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;// 在多线程程序中，新创建的线程不能访问UI线程创建的窗口控件，如果需要访问窗口中的控件，可以在窗口构造函数中将CheckForIllegalCrossThreadCalls设置为 false

            //this.WindowState = FormWindowState.Maximized; // 窗体启动就最大化

            InitEngines();// 初始化引擎

            txtThreshold.Enabled = false;// 阈值不可编辑状态
            getNowTimerAndWeek();// 获取当前时间和星期几

            initComboBox();
        }

        #region 时间
        public void getNowTimerAndWeek() {
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;// 1秒
            timer.Tick += timer_Tick;
            timer.Start();
        }
        void timer_Tick(object sender, EventArgs e) {
            string nowTime = DateTime.Now.ToString();
            this.labelNowTimeAndWeek.Text = nowTime + "   " + getDayOfWeek();
        }
        public string getDayOfWeek() {
            string[] weekdays = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
            string week = weekdays[Convert.ToInt32(DateTime.Now.DayOfWeek)];// 强制转为int
            return week;
        }
        #endregion



        /// <summary>
        /// 初始化引擎
        /// </summary>
        private void InitEngines() {
            //读取配置文件
            AppSettingsReader reader = new AppSettingsReader();
            string appId = (string)reader.GetValue("APP_ID", typeof(string));
            string sdkKey64 = (string)reader.GetValue("SDKKEY64", typeof(string));
            string sdkKey32 = (string)reader.GetValue("SDKKEY32", typeof(string));

            var is64CPU = Environment.Is64BitProcess;
            if (is64CPU)// 64位
            {
                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(sdkKey64)) {
                    chooseMultiImgBtn.Enabled = false;
                    matchBtn.Enabled = false;
                    btnClearFaceList.Enabled = false;
                    chooseImgBtn.Enabled = false;
                    MessageBox.Show("请在App.config配置文件中先配置APP_ID和SDKKEY64!");
                    return;
                }
            }
            else// 32位
            {
                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(sdkKey32)) {
                    chooseMultiImgBtn.Enabled = false;
                    matchBtn.Enabled = false;
                    btnClearFaceList.Enabled = false;
                    chooseImgBtn.Enabled = false;
                    MessageBox.Show("请在App.config配置文件中先配置APP_ID和SDKKEY32!");
                    return;
                }
            }

            //激活引擎    如出现错误，1.请先确认从官网下载的sdk库已放到对应的bin中，2.当前选择的CPU为x86或者x64
            int retCode = 0;

            try {
                retCode = ASFFunctions.ASFActivation(appId, is64CPU ? sdkKey64 : sdkKey32);
            }
            catch (Exception ex) {
                chooseMultiImgBtn.Enabled = false;
                matchBtn.Enabled = false;
                btnClearFaceList.Enabled = false;
                chooseImgBtn.Enabled = false;
                if (ex.Message.IndexOf("无法加载 DLL") > -1) {
                    MessageBox.Show("请将sdk相关DLL放入bin对应的x86或x64下的文件夹中!");
                }
                else {
                    MessageBox.Show("激活引擎失败!");
                }
                return;
            }
            Console.WriteLine("Activate Result:" + retCode);

            //初始化引擎
            uint detectMode = DetectionMode.ASF_DETECT_MODE_IMAGE;
            //检测脸部的角度优先值
            int detectFaceOrientPriority = ASF_OrientPriority.ASF_OP_0_HIGHER_EXT;
            //人脸在图片中所占比例，如果需要调整检测人脸尺寸请修改此值，有效数值为2-32
            int detectFaceScaleVal = 16;
            //最大需要检测的人脸个数
            int detectFaceMaxNum = 10;
            //引擎初始化时需要初始化的检测功能组合
            int combinedMask = FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION | FaceEngineMask.ASF_AGE | FaceEngineMask.ASF_GENDER | FaceEngineMask.ASF_FACE3DANGLE;
            //初始化引擎，正常值为0，其他返回值请参考http://ai.arcsoft.com.cn/bbs/forum.php?mod=viewthread&tid=19&_dsign=dbad527e
            retCode = ASFFunctions.ASFInitEngine(detectMode, detectFaceOrientPriority, detectFaceScaleVal, detectFaceMaxNum, combinedMask, ref pImageEngine);
            Console.WriteLine("引擎初始化结果:" + retCode);
            AppendText((retCode == 0) ? "引擎初始化成功!\n" : string.Format("引擎初始化失败!错误码为:{0}\n", retCode));
            if (retCode != 0) {
                chooseMultiImgBtn.Enabled = false;
                matchBtn.Enabled = false;
                btnClearFaceList.Enabled = false;
                chooseImgBtn.Enabled = false;
            }


            //初始化视频模式下人脸检测引擎
            uint detectModeVideo = DetectionMode.ASF_DETECT_MODE_VIDEO;
            int combinedMaskVideo = FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION;
            retCode = ASFFunctions.ASFInitEngine(detectModeVideo, detectFaceOrientPriority, detectFaceScaleVal, detectFaceMaxNum, combinedMaskVideo, ref pVideoEngine);

            //视频专用FR引擎
            detectFaceMaxNum = 1;
            combinedMask = FaceEngineMask.ASF_FACERECOGNITION | FaceEngineMask.ASF_FACE3DANGLE;
            retCode = ASFFunctions.ASFInitEngine(detectMode, detectFaceOrientPriority, detectFaceScaleVal, detectFaceMaxNum, combinedMask, ref pVideoImageEngine);
            Console.WriteLine("InitVideoEngine Result:" + retCode);
        }

        /// <summary>
        /// “选择识别图片”按钮事件
        /// </summary>
        private void ChooseImg(object sender, EventArgs e) {
            lblCompareInfo.Text = "";
            if (pImageEngine == IntPtr.Zero) {
                chooseMultiImgBtn.Enabled = false;
                matchBtn.Enabled = false;
                btnClearFaceList.Enabled = false;
                chooseImgBtn.Enabled = false;
                MessageBox.Show("请先初始化引擎!");
                return;
            }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "选择图片";
            openFileDialog.Filter = "图片文件|*.bmp;*.jpg;*.jpeg;*.png";
            openFileDialog.Multiselect = false;
            openFileDialog.FileName = string.Empty;
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                DateTime detectStartTime = DateTime.Now;
                AppendText(string.Format("------------------------------开始检测，时间:{0}------------------------------\n", detectStartTime.ToString("yyyy-MM-dd HH:mm:ss:ms")));
                image1Path = openFileDialog.FileName;

                //获取文件，拒绝过大的图片
                FileInfo fileInfo = new FileInfo(image1Path);
                long maxSize = 1024 * 1024 * 2;
                if (fileInfo.Length > maxSize) {
                    MessageBox.Show("图像文件最大为2MB，请压缩后再导入!");
                    AppendText(string.Format("------------------------------检测结束，时间:{0}------------------------------\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ms")));
                    AppendText("\n");
                    return;
                }

                Image srcImage = Image.FromFile(image1Path);
                //调整图像宽度，需要宽度为4的倍数
                if (srcImage.Width % 4 != 0) {
                    //srcImage = ImageUtil.ScaleImage(srcImage, picImageCompare.Width, picImageCompare.Height);
                    srcImage = ImageUtil.ScaleImage(srcImage, srcImage.Width - (srcImage.Width % 4), srcImage.Height);
                }
                //调整图片数据，非常重要
                ImageInfo imageInfo = ImageUtil.ReadBMP(srcImage);
                //人脸检测
                ASF_MultiFaceInfo multiFaceInfo = FaceUtil.DetectFace(pImageEngine, imageInfo);
                //年龄检测
                int retCode_Age = -1;
                ASF_AgeInfo ageInfo = FaceUtil.AgeEstimation(pImageEngine, imageInfo, multiFaceInfo, out retCode_Age);
                //性别检测
                int retCode_Gender = -1;
                ASF_GenderInfo genderInfo = FaceUtil.GenderEstimation(pImageEngine, imageInfo, multiFaceInfo, out retCode_Gender);

                //3DAngle检测
                int retCode_3DAngle = -1;
                ASF_Face3DAngle face3DAngleInfo = FaceUtil.Face3DAngleDetection(pImageEngine, imageInfo, multiFaceInfo, out retCode_3DAngle);

                MemoryUtil.Free(imageInfo.imgData);

                if (multiFaceInfo.faceNum < 1) {
                    srcImage = ImageUtil.ScaleImage(srcImage, picImageCompare.Width, picImageCompare.Height);
                    image1Feature = IntPtr.Zero;
                    picImageCompare.Image = srcImage;
                    AppendText(string.Format("{0} - 未检测出人脸!\n\n", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
                    AppendText(string.Format("------------------------------检测结束，时间:{0}------------------------------\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ms")));
                    AppendText("\n");
                    return;
                }

                MRECT temp = new MRECT();
                int ageTemp = 0;
                int genderTemp = 0;
                int rectTemp = 0;

                //标记出检测到的人脸
                for (int i = 0; i < multiFaceInfo.faceNum; i++) {
                    MRECT rect = MemoryUtil.PtrToStructure<MRECT>(multiFaceInfo.faceRects + MemoryUtil.SizeOf<MRECT>() * i);
                    int orient = MemoryUtil.PtrToStructure<int>(multiFaceInfo.faceOrients + MemoryUtil.SizeOf<int>() * i);
                    int age = 0;

                    if (retCode_Age != 0) {
                        AppendText(string.Format("年龄检测失败，返回{0}!\n\n", retCode_Age));
                    }
                    else {
                        age = MemoryUtil.PtrToStructure<int>(ageInfo.ageArray + MemoryUtil.SizeOf<int>() * i);
                    }

                    int gender = -1;
                    if (retCode_Gender != 0) {
                        AppendText(string.Format("性别检测失败，返回{0}!\n\n", retCode_Gender));
                    }
                    else {
                        gender = MemoryUtil.PtrToStructure<int>(genderInfo.genderArray + MemoryUtil.SizeOf<int>() * i);
                    }


                    int face3DStatus = -1;
                    float roll = 0f;
                    float pitch = 0f;
                    float yaw = 0f;
                    if (retCode_3DAngle != 0) {
                        AppendText(string.Format("3DAngle检测失败，返回{0}!\n\n", retCode_3DAngle));
                    }
                    else {
                        //角度状态 非0表示人脸不可信
                        face3DStatus = MemoryUtil.PtrToStructure<int>(face3DAngleInfo.status + MemoryUtil.SizeOf<int>() * i);
                        //roll为侧倾角，pitch为俯仰角，yaw为偏航角
                        roll = MemoryUtil.PtrToStructure<float>(face3DAngleInfo.roll + MemoryUtil.SizeOf<float>() * i);
                        pitch = MemoryUtil.PtrToStructure<float>(face3DAngleInfo.pitch + MemoryUtil.SizeOf<float>() * i);
                        yaw = MemoryUtil.PtrToStructure<float>(face3DAngleInfo.yaw + MemoryUtil.SizeOf<float>() * i);
                    }


                    int rectWidth = rect.right - rect.left;
                    int rectHeight = rect.bottom - rect.top;

                    //查找最大人脸
                    if (rectWidth * rectHeight > rectTemp) {
                        rectTemp = rectWidth * rectHeight;
                        temp = rect;
                        ageTemp = age;
                        genderTemp = gender;
                    }

                    //srcImage = ImageUtil.MarkRectAndString(srcImage, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, age, gender);
                    AppendText(string.Format("{0} - 人脸坐标:[left:{1},top:{2},right:{3},bottom:{4},orient:{5},roll:{6},pitch:{7},yaw:{8},status:{11}] Age:{9} Gender:{10}\n", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), rect.left, rect.top, rect.right, rect.bottom, orient, roll, pitch, yaw, age, (gender >= 0 ? gender.ToString() : ""), face3DStatus));
                }


                AppendText(string.Format("{0} - 人脸数量:{1}\n\n", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), multiFaceInfo.faceNum));


                DateTime detectEndTime = DateTime.Now;// 获取当前的时间
                AppendText(string.Format("------------------------------检测结束，时间:{0}------------------------------\n", detectEndTime.ToString("yyyy-MM-dd HH:mm:ss:ms")));
                AppendText("\n");
                ASF_SingleFaceInfo singleFaceInfo = new ASF_SingleFaceInfo();// 单人脸检测
                //提取人脸特征
                image1Feature = FaceUtil.ExtractFeature(pImageEngine, srcImage, out singleFaceInfo);

                //清空上次的匹配结果
                for (int i = 0; i < imagesFeatureList.Count; i++) {
                    imageList.Items[i].Text = string.Format("{0}号", i);
                }

                float scaleRate = ImageUtil.getWidthAndHeight(srcImage.Width, srcImage.Height, picImageCompare.Width, picImageCompare.Height);
                srcImage = ImageUtil.ScaleImage(srcImage, picImageCompare.Width, picImageCompare.Height);
                srcImage = ImageUtil.MarkRectAndString(srcImage, (int)(temp.left * scaleRate), (int)(temp.top * scaleRate), (int)(temp.right * scaleRate) - (int)(temp.left * scaleRate), (int)(temp.bottom * scaleRate) - (int)(temp.top * scaleRate), ageTemp, genderTemp, picImageCompare.Width);


                //显示标记后的图像
                picImageCompare.Image = srcImage;
            }
        }


        private object locker = new object();

        /// <summary>
        /// 人脸库图片选择按钮事件
        /// </summary>
        private void ChooseMultiImg(object sender, EventArgs e) {
            lock (locker) {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "选择图片";
                openFileDialog.Filter = "图片文件|*.bmp;*.jpg;*.jpeg;*.png";
                openFileDialog.Multiselect = true;
                openFileDialog.FileName = string.Empty;
                if (openFileDialog.ShowDialog() == DialogResult.OK)// 用户一点击“确定”按钮，那么对话框就关闭，重新回到主窗体，然后可以在主窗体中进行相应的处理，比如把数据写入数据库等
                {

                    List<string> imagePathListTemp = new List<string>();
                    var numStart = imagePathList.Count;
                    int isGoodImage = 0;

                    //保存图片路径并显示
                    // TODO 这边将从数据库获取人脸地址
                    string[] fileNames = openFileDialog.FileNames;
                    Console.WriteLine(fileNames.ToString());
                    for (int i = 0; i < fileNames.Length; i++) {
                        imagePathListTemp.Add(fileNames[i]);// 将数据库查处的地址列表存入temp
                    }



                    //人脸检测以及提取人脸特征
                    ThreadPool.QueueUserWorkItem(new WaitCallback(delegate {
                        //禁止点击按钮
                        Invoke(new Action(delegate {
                            chooseMultiImgBtn.Enabled = false;
                            matchBtn.Enabled = false;
                            btnClearFaceList.Enabled = false;
                            chooseImgBtn.Enabled = false;
                            btnStartVideo.Enabled = false;
                        }));

                        //人脸检测和剪裁
                        for (int i = 0; i < imagePathListTemp.Count; i++) {
                            Image image = Image.FromFile(imagePathListTemp[i]);
                            if (image.Width % 4 != 0) {
                                image = ImageUtil.ScaleImage(image, image.Width - (image.Width % 4), image.Height);
                            }
                            ASF_MultiFaceInfo multiFaceInfo = FaceUtil.DetectFace(pImageEngine, image);

                            if (multiFaceInfo.faceNum > 0) {
                                imagePathList.Add(imagePathListTemp[i]);
                                MRECT rect = MemoryUtil.PtrToStructure<MRECT>(multiFaceInfo.faceRects);
                                image = ImageUtil.CutImage(image, rect.left, rect.top, rect.right, rect.bottom);
                            }
                            else {
                                continue;
                            }

                            this.Invoke(new Action(delegate {
                                if (image == null) {
                                    image = Image.FromFile(imagePathListTemp[i]);
                                }
                                imageLists.Images.Add(imagePathListTemp[i], image);
                                imageList.Items.Add((numStart + isGoodImage) + "号", imagePathListTemp[i]);
                                isGoodImage += 1;
                                image = null;
                            }));
                        }


                        //提取人脸特征
                        for (int i = numStart; i < imagePathList.Count; i++) {
                            ASF_SingleFaceInfo singleFaceInfo = new ASF_SingleFaceInfo();
                            // TODO  将feature存入数据库,byte[]，这边已经在FaceUtils中处理了
                            IntPtr feature = FaceUtil.ExtractFeature(pImageEngine, Image.FromFile(imagePathList[i]), out singleFaceInfo);// 人脸特征
                            this.Invoke(new Action(delegate {
                                if (singleFaceInfo.faceRect.left == 0 && singleFaceInfo.faceRect.right == 0) {
                                    AppendText(string.Format("{0}号未检测到人脸\r\n", i));
                                }
                                else {

                                    AppendText(string.Format("已提取{0}号人脸特征值，[left:{1},right:{2},top:{3},bottom:{4},orient:{5}]\r\n", i, singleFaceInfo.faceRect.left, singleFaceInfo.faceRect.right, singleFaceInfo.faceRect.top, singleFaceInfo.faceRect.bottom, singleFaceInfo.faceOrient));
                                    imagesFeatureList.Add(feature);

                                    // TODO 数据库中查询出feature
                                    /*
                                     IntPtr pFeatureItemFromDB;
                                    MysqlUtils mysqlUtils = new MysqlUtils();
                                    List<byte[]> featureListFromDB = mysqlUtils.SelectUserFaceByFeature();
                                    foreach (byte[] featureItemFromDB in featureListFromDB)
                                    {
                                        pFeatureItemFromDB = TabConvert.BytesToIntptr(featureItemFromDB);
                                        imagesFeatureList.Add(pFeatureItemFromDB);
                                    }
                                     */

                                }
                            }));
                        }
                        //允许点击按钮
                        Invoke(new Action(delegate {
                            chooseMultiImgBtn.Enabled = true;
                            btnClearFaceList.Enabled = true;
                            btnStartVideo.Enabled = true;

                            if (btnStartVideo.Text == "启用摄像头") {
                                chooseImgBtn.Enabled = true;
                                matchBtn.Enabled = true;
                            }
                            else {
                                chooseImgBtn.Enabled = false;
                                matchBtn.Enabled = false;
                            }
                        }));
                    }));

                }
            }
        }

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        private void Form_Closed(object sender, FormClosedEventArgs e) {
            //销毁引擎
            int retCode = ASFFunctions.ASFUninitEngine(pImageEngine);
            Console.WriteLine("UninitEngine pImageEngine Result:" + retCode);
            //销毁引擎
            retCode = ASFFunctions.ASFUninitEngine(pVideoEngine);
            Console.WriteLine("UninitEngine pVideoEngine Result:" + retCode);

        }

        /// <summary>
        /// 追加公用方法
        /// </summary>
        /// <param name="message"></param>
        private void AppendText(string message) {
            logBox.AppendText(message);
        }

        /// <summary>
        /// 匹配事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void matchBtn_Click(object sender, EventArgs e) {
            if (imagesFeatureList.Count == 0) {
                MessageBox.Show("请注册人脸!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (image1Feature == IntPtr.Zero) {
                if (picImageCompare.Image == null) {
                    MessageBox.Show("请选择识别图!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else {
                    MessageBox.Show("比对失败，识别图未提取到特征值!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            //标记已经做了匹配比对，在开启视频的时候要清除比对结果
            isCompare = true;
            float compareSimilarity = 0f;
            int compareNum = 0;
            AppendText(string.Format("------------------------------开始比对，时间:{0}------------------------------\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ms")));
            for (int i = 0; i < imagesFeatureList.Count; i++) {
                IntPtr feature = imagesFeatureList[i];
                float similarity = 0f;
                int ret = ASFFunctions.ASFFaceFeatureCompare(pImageEngine, image1Feature, feature, ref similarity);
                //增加异常值处理
                if (similarity.ToString().IndexOf("E") > -1) {
                    similarity = 0f;
                }
                AppendText(string.Format("与{0}号比对结果:{1}\r\n", i, similarity));
                imageList.Items[i].Text = string.Format("{0}号({1})", i, similarity);
                if (similarity > compareSimilarity) {
                    compareSimilarity = similarity;
                    compareNum = i;
                }
            }
            if (compareSimilarity > 0) {
                lblCompareInfo.Text = " " + compareNum + "号," + compareSimilarity;
            }
            AppendText(string.Format("------------------------------比对结束，时间:{0}------------------------------\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ms")));
        }

        /// <summary>
        /// 清除人脸库事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearFaceList_Click(object sender, EventArgs e) {
            //清除数据
            imageLists.Images.Clear();
            imageList.Items.Clear();
            imagesFeatureList.Clear();
            imagePathList.Clear();
        }



        private FaceTrackUnit trackUnit = new FaceTrackUnit();
        private Font font = new Font(FontFamily.GenericSerif, 10f);
        private SolidBrush brush = new SolidBrush(Color.Red);
        private Pen pen = new Pen(Color.Red);
        private bool isLock = false;



        /// <summary>
        /// 得到feature比较结果
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        private int compareFeature(IntPtr feature, out float similarity) {
            int result = -1;
            similarity = 0f;
            //如果人脸库不为空，则进行人脸匹配
            if (imagesFeatureList != null && imagesFeatureList.Count > 0) {
                for (int i = 0; i < imagesFeatureList.Count; i++) {
                    //调用人脸匹配方法，进行人脸特征匹配
                    ASFFunctions.ASFFaceFeatureCompare(pVideoImageEngine, feature, imagesFeatureList[i], ref similarity);
                    if (similarity >= threshold)// 相似度大于阈值，输出人脸序号，暂时默认阈值为0.8
                    {
                        result = i;
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 得到feature比较结果
        /// </summary>
        /// <param name="pVideoFaceFeature"></param>
        /// <returns></returns>
        private int compareFeatureFromDB(IntPtr pVideoFaceFeature, out float similarity) {
            int result = -1;
            similarity = 0f;


            //如果人脸库不为空，则进行人脸匹配
            if (imagesFeatureList != null && imagesFeatureList.Count > 0) {
                for (int i = 0; i < imagesFeatureList.Count; i++) {
                    //调用人脸匹配方法，进行人脸特征匹配
                    ASFFunctions.ASFFaceFeatureCompare(pVideoImageEngine, pVideoFaceFeature, imagesFeatureList[i], ref similarity);
                    if (similarity >= threshold)// 相似度大于阈值，输出人脸序号，暂时默认阈值为0.8
                    {
                        result = i;
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 阈值文本框键按下事件，检测输入内容是否正确，不正确不能输入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtThreshold_KeyPress(object sender, KeyPressEventArgs e) {
            //阻止从键盘输入键
            e.Handled = true;
            //是数值键，回退键，.能输入，其他不能输入
            if (char.IsDigit(e.KeyChar) || e.KeyChar == 8 || e.KeyChar == '.') {
                //渠道当前文本框的内容
                string thresholdStr = txtThreshold.Text.Trim();
                int countStr = 0;
                int startIndex = 0;
                //如果当前输入的内容是否是“.”
                if (e.KeyChar == '.') {
                    countStr = 1;
                }
                //检测当前内容是否含有.的个数
                if (thresholdStr.IndexOf('.', startIndex) > -1) {
                    countStr += 1;
                }
                //如果输入的内容已经超过12个字符，
                if (e.KeyChar != 8 && (thresholdStr.Length > 12 || countStr > 1)) {
                    return;
                }
                e.Handled = false;
            }
        }

        /// <summary>
        /// 阈值文本框键抬起事件，检测阈值是否正确，不正确改为0.8f
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtThreshold_KeyUp(object sender, KeyEventArgs e) {
            //如果输入的内容不正确改为默认值
            if (!float.TryParse(txtThreshold.Text.Trim(), out threshold)) {
                threshold = 0.8f;
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            ChannelInfo channelInfo = new ChannelInfo();
            channelInfo.channelId = SidUtils.sid();
            channelInfo.channelDatabaseName = "dycommondatabase30";
            channelInfo.channelDatabaseType = 1;
            channelInfo.channelDatabasePassword = "1100110";
            channelInfo.channelServerName = "192.168.138.45";
            channelInfo.channelUserName = "sa";
            channelInfo.channelName = "城市高清网";
            InitChannel initChannel = new InitChannel();
            //initChannel.AddChannel(channelInfo);
            //initChannel.QueryChannels();
            List<ChannelInfo> lsit = InitChannel.QueryChannels();
            Console.WriteLine("124");

        }

        private void initComboBox() {
            List<ChannelInfo> channelInfoList = InitChannel.QueryChannels();
            foreach (var item in channelInfoList) {
                comboBox1.Items.Add(item.channelName);
            }

        }
    }
}
