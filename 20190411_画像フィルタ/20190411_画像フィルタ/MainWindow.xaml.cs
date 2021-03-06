﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _20190411_画像フィルタ
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        byte[] MyPixelsOrigin;//元画像、リセット用
        byte[] MyPixels;//一時保存用、エッジを元にぼかすときに使う
        byte[] MyEdge;//エッジ抽出した画像、フィルタ用
        byte[] MyPixels膨張収縮;//
        byte[] MyFiltered;//フィルタを掛けた画像＆フィルタを掛ける用の画像

        BitmapSource MyBitmap;
        string ImageFileFullPath;
        //エッジ抽出、ぼかし、エッジ抽出、ぼかし
        //元を使ってエッジ抽出、元にエッジを使ってぼかし、ぼかしたものからエッジ抽出、ぼかしたものをエッジを使ってぼかし
        //

        public MainWindow()
        {
            InitializeComponent();
            AllowDrop = true;
            Drop += MainWindow_Drop;

            MyTest();
        }

        private void MyTest()
        {
            //string filePath = "";
            ImageFileFullPath = @"E:\オレ\雑誌スキャン\2003年pc雑誌\20030115_dosvmag_003.jpg";
            //ImageFileFullPath = @"D:\ブログ用\テスト用画像\ノイズ除去\20030115_dosvmag_114.jpg";
            //ImageFileFullPath = @" D:\ブログ用\テスト用画像\border_row.bmp";
            //ImageFileFullPath = @"D:\ブログ用\テスト用画像\ノイズ除去\20030115_dosvmag_003_.png";
            ImageFileFullPath = @"D:\ブログ用\テスト用画像\ノイズ除去\20030115_dosvmag_003_重.png";
            ImageFileFullPath = @"D:\ブログ用\テスト用画像\ノイズ除去\20030115_dosvmag_003_重_上半分.png";
            //ImageFileFullPath = @"D:\ブログ用\Lenna_(test_image).png";


            (MyPixels, MyBitmap) = MakeBitmapSourceAndByteArray(ImageFileFullPath, PixelFormats.Gray8, 96, 96);

            MyImageOrigin.Source = MyBitmap;
            MyPixelsOrigin = MyPixels;
            MyPixels膨張収縮 = MyPixels;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="weight"></param>
        /// <param name="div"></param>
        /// <param name="offset"></param>
        /// <param name="absolute">ラプラシアンは絶対値で計算したほうがいい気がする</param>
        /// <returns></returns>
        private (byte[] pixels, BitmapSource bitmap) Filter(byte[] pixels, int width, int height, int[][] weight, int div, int offset, bool absolute = false)
        {
            //int[][] weight = new int[][] {
            //    new int[] { 0, 1, 0 },
            //    new int[] { 1, 1, 1 },
            //    new int[] { 0, 1, 0 } };
            //int offset = 0;
            //int div = 5;
            byte[] filtered = new byte[pixels.Length];
            int p;
            //めんどくさいので上下左右1ピクセルは処理しない
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    double v = 0.0;
                    p = x + y * width;
                    int pp;
                    for (int a = 0; a < 3; a++)
                    {
                        for (int b = 0; b < 3; b++)
                        {
                            pp = (x + b - 1) + ((y + a - 1) * width);
                            v += pixels[pp] * weight[a][b];
                        }
                    }
                    if (absolute) { v = Math.Abs(v); }
                    //v = Math.Abs(v);
                    v /= div;
                    v += offset;
                    v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                    filtered[p] = (byte)v;
                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }
        //速度は↑より誤差程度遅いので、わかりやすい↑を使ったほうがいい
        private (byte[] pixesl, BitmapSource bitmap) Filter2(byte[] pixels, int width, int height, int[][] weight, int div, int offset)
        {
            byte[] filtered = new byte[pixels.Length];
            int stride = 1 * width;

            //めんどくさいので上下左右1ピクセルは処理しない            
            for (int i = stride + 1; i < pixels.Length - stride - 1; i++)
            {
                //右端ならcount＋2して次へ
                int mod = i % stride;
                if (mod == width - 1) { i += 2; continue; }

                double v = 0.0;
                int pp;
                for (int a = -1; a < 2; a++)
                {
                    for (int b = -1; b < 2; b++)
                    {
                        pp = i + b + (a * stride);
                        v += pixels[pp] * weight[a + 1][b + 1];
                    }
                }
                v = Math.Abs(v);
                v /= div;
                v += offset;
                v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                filtered[i] = (byte)v;

            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) Filter5x5(byte[] pixels, int width, int height, int[][] weight, int div, int offset)
        {
            //カーネルサイズが5x5
            byte[] filtered = new byte[pixels.Length];
            int p;
            //上下左右2ピクセルラインは処理しない
            for (int y = 2; y < height - 2; y++)
            {
                for (int x = 2; x < width - 2; x++)
                {
                    double v = 0.0;
                    p = x + y * width;
                    int pp;
                    for (int a = 0; a < 5; a++)
                    {
                        for (int b = 0; b < 5; b++)
                        {
                            pp = (x + b - 2) + ((y + a - 2) * width);
                            v += pixels[pp] * weight[a][b];
                        }
                    }
                    //v = Math.Abs(v);
                    v /= div;
                    v += offset;
                    v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                    filtered[p] = (byte)v;
                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }

        //メディアンフィルタ、自身と周辺8画素の中央値をそのまま適用
        //ノイズ除去の効果があるけど、かなりぼやける
        //処理時間はぼかしの10倍
        private (byte[] pixesl, BitmapSource bitmap) MedianFilter(byte[] pixels, int width, int height)
        {
            byte[] filtered = new byte[pixels.Length];
            //めんどくさいので上下左右1ピクセルは処理しない
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pp;
                    List<byte> sort = new List<byte>();
                    for (int a = 0; a < 3; a++)
                    {
                        for (int b = 0; b < 3; b++)
                        {
                            pp = (x + b - 1) + ((y + a - 1) * width);
                            sort.Add(pixels[pp]);
                        }
                    }
                    //ソートして中央値を採用
                    var sorted = sort.OrderBy(z => z);
                    filtered[x + y * width] = sorted.ToList()[4];
                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }

        private (byte[] pixels, BitmapSource bitmap) SobelFilterソーベルフィルタ(byte[] pixels, int width, int height)
        {
            //int[] xWeight = new int[] {
            //    -1, 0, 1,
            //    -2, 0, 2,
            //    -1, 0, 1 };
            //int[] yWeight = new int[] {
            //    -1, -2, -1,
            //    0, 0, 0,
            //    1, 2, 1 };

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vX, vY;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    vX = vY = 0;
                    p = x + y * width;
                    //中段X
                    vX += pixels[p - 1] * -2;
                    vX += pixels[p + 1] * 2;
                    //上段X
                    vX += pixels[p - width - 1] * -1;
                    vX += pixels[p - width + 1] * 1;
                    //下段X
                    vX += pixels[p + width - 1] * -1;
                    vX += pixels[p + width + 1] * 1;

                    //上段Y
                    vY += pixels[p - width - 1] * -1;
                    vY += pixels[p - width] * -2;
                    vY += pixels[p - width + 1] * -1;
                    //下段Y
                    vY += pixels[p + width - 1] * 1;
                    vY += pixels[p + width] * 2;
                    vY += pixels[p + width + 1] * 1;

                    int v = Math.Abs(vX) + Math.Abs(vY);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) SobelFilterソーベルフィルタ縦(byte[] pixels, int width, int height)
        {
            //int[] yWeight = new int[] {
            //    -1, -2, -1,
            //    0, 0, 0,
            //    1, 2, 1 };

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vY;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    vY = 0;
                    p = x + y * width;

                    //上段Y
                    vY += pixels[p - width - 1] * -1;
                    vY += pixels[p - width] * -2;
                    vY += pixels[p - width + 1] * -1;
                    //下段Y
                    vY += pixels[p + width - 1] * 1;
                    vY += pixels[p + width] * 2;
                    vY += pixels[p + width + 1] * 1;

                    int v = Math.Abs(vY);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) SobelFilterソーベルフィルタ横(byte[] pixels, int width, int height)
        {
            //int[] xWeight = new int[] {
            //    -1, 0, 1,
            //    -2, 0, 2,
            //    -1, 0, 1 };

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vX;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    vX = 0;
                    p = x + y * width;
                    //中段X
                    vX += pixels[p - 1] * -2;
                    vX += pixels[p + 1] * 2;
                    //上段X
                    vX += pixels[p - width - 1] * -1;
                    vX += pixels[p - width + 1] * 1;
                    //下段X
                    vX += pixels[p + width - 1] * -1;
                    vX += pixels[p + width + 1] * 1;

                    int v = Math.Abs(vX);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) SobelFilterソーベルフィルタ2乗和の平方根(byte[] pixels, int width, int height)
        {
            //int[] xWeight = new int[] {
            //    -1, 0, 1,
            //    -2, 0, 2,
            //    -1, 0, 1 };
            //int[] yWeight = new int[] {
            //    -1, -2, -1,
            //    0, 0, 0,
            //    1, 2, 1 };

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vX, vY;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    vX = vY = 0;
                    p = x + y * width;
                    //中段X
                    vX += pixels[p - 1] * -2;
                    vX += pixels[p + 1] * 2;
                    //上段X
                    vX += pixels[p - width - 1] * -1;
                    vX += pixels[p - width + 1] * 1;
                    //下段X
                    vX += pixels[p + width - 1] * -1;
                    vX += pixels[p + width + 1] * 1;

                    //上段Y
                    vY += pixels[p - width - 1] * -1;
                    vY += pixels[p - width] * -2;
                    vY += pixels[p - width + 1] * -1;
                    //下段Y
                    vY += pixels[p + width - 1] * 1;
                    vY += pixels[p + width] * 2;
                    vY += pixels[p + width + 1] * 1;

                    int v = (int)Math.Sqrt(vX * vX + vY * vY);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;
                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) SobelFilterソーベルフィルタ斜め和(byte[] pixels, int width, int height)
        {
            //    A            B
            // 0,  1, 2     2,  1,  0
            //-1,  0, 1     1,  0, -1
            //-2, -1, 0     0, -1, -2

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vA, vB;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    vA = vB = 0;
                    p = x + y * width;//中心Index
                    //a上段
                    vA += pixels[p - width];//中
                    vA += pixels[p - width + 1] * 2;//右
                    //a中段
                    vA += pixels[p - 1] * -1;//左
                    vA += pixels[p + 1];//右
                    //a下段
                    vA += pixels[p + width - 1] * -2;//左
                    vA += pixels[p + width] * -1;//中

                    //b上段
                    vB += pixels[p - width - 1] * 2;//左
                    vB += pixels[p - width];//中
                    //b中断
                    vB += pixels[p - 1];//左
                    vB += pixels[p + 1] * -1;//右
                    //b下段
                    vB += pixels[p + width] * -1;//中
                    vB += pixels[p + width + 1] * -2;//右

                    //和
                    int v = Math.Abs(vA) + Math.Abs(vB);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) SobelFilterソーベルフィルタ斜め2乗和の平方根(byte[] pixels, int width, int height)
        {
            //    A            B
            // 0,  1, 2     2,  1,  0
            //-1,  0, 1     1,  0, -1
            //-2, -1, 0     0, -1, -2

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vA, vB;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    vA = vB = 0;
                    p = x + y * width;//中心Index
                    //a上段
                    vA += pixels[p - width];//中
                    vA += pixels[p - width + 1] * 2;//右
                    //a中段
                    vA += pixels[p - 1] * -1;//左
                    vA += pixels[p + 1];//右
                    //a下段
                    vA += pixels[p + width - 1] * -2;//左
                    vA += pixels[p + width] * -1;//中

                    //b上段
                    vB += pixels[p - width - 1] * 2;//左
                    vB += pixels[p - width];//中
                    //b中断
                    vB += pixels[p - 1];//左
                    vB += pixels[p + 1] * -1;//右
                    //b下段
                    vB += pixels[p + width] * -1;//中
                    vB += pixels[p + width + 1] * -2;//右

                    //2乗和の平方根
                    int v = (int)Math.Sqrt(vA * vA + vB * vB);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) EdgeFilter一次微分フィルタ和(byte[] pixels, int width, int height)
        {
            //            【画像処理】一次微分フィルタの原理・特徴・計算式 | アルゴリズム雑記
            //https://algorithm.joho.info/image-processing/differential-filter/

            //    横            縦
            // 0,  0, 0     0, -1,  0
            //-1,  0, 1     0,  0,  0
            // 0,  0, 0     0,  1,  0

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vX, vY;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    p = x + y * width;
                    vX = 0;
                    //上段X

                    //中段X
                    vX += pixels[p - 1] * -1;
                    vX += pixels[p + 1];
                    //下段X

                    vY = 0;
                    //上段Y
                    vY += pixels[p - width] * -1;
                    //中断Y

                    //下段Y
                    vY += pixels[p + width];

                    int v = Math.Abs(vX) + Math.Abs(vY);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) EdgeFilter一次微分フィルタ2乗和の平方根(byte[] pixels, int width, int height)
        {
            //    横            縦
            // 0,  0, 0     0, -1,  0
            //-1,  0, 1     0,  0,  0
            // 0,  0, 0     0,  1,  0

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vX, vY;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    p = x + y * width;
                    vX = 0;
                    //上段X

                    //中段X
                    vX += pixels[p - 1] * -1;
                    vX += pixels[p + 1];
                    //下段X

                    vY = 0;
                    //上段Y
                    vY += pixels[p - width] * -1;
                    //中断Y

                    //下段Y
                    vY += pixels[p + width];

                    int v = (int)Math.Sqrt(vX * vX + vY * vY);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) EdgeFilterプレウィットフィルタ和(byte[] pixels, int width, int height)
        {
            //            【画像処理】プレウィットフィルタの原理・特徴・計算式 | アルゴリズム雑記
            //https://algorithm.joho.info/image-processing/prewitt-filter/

            //    横            縦
            // -1,  0, 1    -1, -1, -1
            // -1,  0, 1     0,  0,  0
            // -1,  0, 1     1,  1,  1

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vX, vY;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    p = x + y * width;
                    vX = 0;//横
                    //上段X
                    vX += pixels[p - width - 1] * -1;
                    vX += pixels[p - width + 1];
                    //中段X
                    vX += pixels[p - 1] * -1;
                    vX += pixels[p + 1];
                    //下段X
                    vX += pixels[p + width - 1] * -1;
                    vX += pixels[p + width + 1];


                    vY = 0;//縦
                    //上段Y
                    vY += pixels[p - width - 1] * -1;
                    vY += pixels[p - width] * -1;
                    vY += pixels[p - width + 1] * -1;
                    //中断Y

                    //下段Y
                    vY += pixels[p + width - 1];
                    vY += pixels[p + width];
                    vY += pixels[p + width + 1];

                    //和
                    int v = Math.Abs(vX) + Math.Abs(vY);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) EdgeFilterプレウィットフィルタ2乗和の平方根(byte[] pixels, int width, int height)
        {
            //    横            縦
            // -1,  0, 1    -1, -1, -1
            // -1,  0, 1     0,  0,  0
            // -1,  0, 1     1,  1,  1

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vX, vY;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    p = x + y * width;
                    vX = 0;//横
                    //上段X
                    vX += pixels[p - width - 1] * -1;
                    vX += pixels[p - width + 1];
                    //中段X
                    vX += pixels[p - 1] * -1;
                    vX += pixels[p + 1];
                    //下段X
                    vX += pixels[p + width - 1] * -1;
                    vX += pixels[p + width + 1];


                    vY = 0;//縦
                    //上段Y
                    vY += pixels[p - width - 1] * -1;
                    vY += pixels[p - width] * -1;
                    vY += pixels[p - width + 1] * -1;
                    //中断Y

                    //下段Y
                    vY += pixels[p + width - 1];
                    vY += pixels[p + width];
                    vY += pixels[p + width + 1];

                    //2乗和の平方根
                    int v = (int)Math.Sqrt(vX * vX + vY * vY);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) EdgeFilterロバーツフィルタ和(byte[] pixels, int width, int height)
        {
            //            画像処理 - HexeRein
            //http://www7a.biglobe.ne.jp/~fairytale/article/program/graphics.html#filtering

            //    横            縦
            //  0,  0,  0     0,  0,  0
            //  0,  1,  0     0,  0,  1
            //  0,  0, -1     0, -1,  0

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vX, vY;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    p = x + y * width;
                    vX = 0;//横
                    //上段X
                    //vX += pixels[p - width - 1] * -1;
                    //vX += pixels[p - width + 1];
                    //中段X
                    //vX += pixels[p - 1] * -1;
                    vX += pixels[p];
                    //vX += pixels[p + 1];
                    //下段X
                    //vX += pixels[p + width - 1] * -1;
                    vX += pixels[p + width + 1] * -1;


                    vY = 0;//縦
                    //上段Y
                    //vY += pixels[p - width - 1] * -1;
                    //vY += pixels[p - width] * -1;
                    //vY += pixels[p - width + 1] * -1;
                    //中段Y
                    vY += pixels[p + 1];
                    //下段Y
                    //vY += pixels[p + width - 1];
                    vY += pixels[p + width] * -1;
                    //vY += pixels[p + width + 1];

                    //和
                    int v = Math.Abs(vX) + Math.Abs(vY);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) EdgeFilterロバーツフィルタ2乗和の平方根(byte[] pixels, int width, int height)
        {
            //            画像処理 - HexeRein
            //http://www7a.biglobe.ne.jp/~fairytale/article/program/graphics.html#filtering

            //    横            縦
            //  0,  0,  0     0,  0,  0
            //  0,  1,  0     0,  0,  1
            //  0,  0, -1     0, -1,  0

            //めんどくさいので上下左右1ラインは処理しない
            byte[] filtered = new byte[pixels.Length];
            int p, vX, vY;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    p = x + y * width;
                    vX = 0;//横
                    //上段X
                    //vX += pixels[p - width - 1] * -1;
                    //vX += pixels[p - width + 1];
                    //中段X
                    //vX += pixels[p - 1] * -1;
                    vX += pixels[p];
                    //vX += pixels[p + 1];
                    //下段X
                    //vX += pixels[p + width - 1] * -1;
                    vX += pixels[p + width + 1] * -1;


                    vY = 0;//縦
                    //上段Y
                    //vY += pixels[p - width - 1] * -1;
                    //vY += pixels[p - width] * -1;
                    //vY += pixels[p - width + 1] * -1;
                    //中段Y
                    vY += pixels[p + 1];
                    //下段Y
                    //vY += pixels[p + width - 1];
                    vY += pixels[p + width] * -1;
                    //vY += pixels[p + width + 1];

                    //和
                    int v = (int)Math.Sqrt(vX * vX + vY * vY);
                    v = (v < 0) ? 0 : (v > 255) ? 255 : v;
                    filtered[p] = (byte)v;

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) ScaleFilter膨張(byte[] pixels, int width, int height)
        {
            //9マス中最大の値にする
            byte[] filtered = new byte[pixels.Length];
            int p;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    p = x + (y * width);
                    filtered[p] =
                        Math.Max(pixels[p - width - 1], Math.Max(pixels[p - width], Math.Max(pixels[p - width + 1],
                        Math.Max(pixels[p - 1], Math.Max(pixels[p], Math.Max(pixels[p + 1],
                        Math.Max(pixels[p + width - 1], Math.Max(pixels[p + width], pixels[p + width + 1]))))))));

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) ScaleFilter収縮(byte[] pixels, int width, int height)
        {
            //9マス中最大の値にする
            byte[] filtered = new byte[pixels.Length];
            int p;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    p = x + (y * width);
                    filtered[p] =
                        Math.Min(pixels[p - width - 1], Math.Min(pixels[p - width], Math.Min(pixels[p - width + 1],
                        Math.Min(pixels[p - 1], Math.Min(pixels[p], Math.Min(pixels[p + 1],
                        Math.Min(pixels[p + width - 1], Math.Min(pixels[p + width], pixels[p + width + 1]))))))));

                }
            }
            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));

        }

        private (byte[] pixels, BitmapSource bitmap) FilterAndFilter(byte[] isFilter, int threshold, byte[] pixels, int width, int height, int[][] weight, int div, int offset)
        {
            //int[][] weight = new int[][] {
            //    new int[] { 0, 1, 0 },
            //    new int[] { 1, 1, 1 },
            //    new int[] { 0, 1, 0 } };
            //int offset = 0;
            //int div = 5;
            byte[] filtered = new byte[pixels.Length];
            int p;

            //上下左右1ピクセルは処理しない
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    double v = 0.0;
                    p = x + y * width;
                    //しきい値以上なら無処理
                    if (threshold < isFilter[p]) { filtered[p] = pixels[p]; continue; }

                    int pp;
                    for (int a = 0; a < 3; a++)
                    {
                        for (int b = 0; b < 3; b++)
                        {
                            pp = (x + b - 1) + ((y + a - 1) * width);
                            v += pixels[pp] * weight[a][b];
                        }
                    }
                    //v = Math.Abs(v);
                    v /= div;
                    v += offset;
                    v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                    filtered[p] = (byte)v;
                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }


        private (byte[] pixels, BitmapSource bitmap) MyFilterHorizontal(byte[] pixels, int width, int height)
        {
            //2つ右との差分だけ計算
            byte[] filtered = new byte[pixels.Length];
            int p;
            for (int y = 0; y < height; y++)
            {
                //右2ピクセルラインは処理しない
                for (int x = 0; x < width - 2; x++)
                {
                    p = x + y * width;
                    //v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                    filtered[p] = (byte)Math.Abs(pixels[p] - pixels[p + 2]);
                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }

        private (byte[] pixels, BitmapSource bitmap) MyFilterVertical(byte[] pixels, int width, int height)
        {
            //2つ下との差分だけ計算
            byte[] filtered = new byte[pixels.Length];
            int p;
            int bottom2line = width * 2;
            //下2ピクセルラインは処理しない
            for (int y = 0; y < height - 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    p = x + y * width;
                    //v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                    filtered[p] = (byte)Math.Abs(pixels[p] - pixels[p + bottom2line]);
                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }

        private (byte[] pixels, BitmapSource bitmap) MyFilter5x5エッジ抽出1マス先(byte[] pixels, int width, int height)
        {
            //外周の4ライン平均との差から一番小さい値
            //
            byte[] filtered = new byte[pixels.Length];
            int p;
            int line1 = width;
            int lines2 = width * 2;

            //2ピクセルラインは処理しない
            for (int y = 2; y < height - 2; y++)
            {
                for (int x = 2; x < width - 2; x++)
                {
                    p = x + y * width;
                    byte current = pixels[p];//中心
                    //1マス離れた4ラインとの差
                    double upper = Math.Abs(current - ((pixels[p - lines2 - 2]
                                                       + pixels[p - lines2 - 1]
                                                       + pixels[p - lines2]
                                                       + pixels[p - lines2 + 1]
                                                       + pixels[p - lines2 + 2]) / 5.0));
                    double lower = Math.Abs(current - ((pixels[p + lines2 - 2]
                                                        + pixels[p + lines2 - 1]
                                                        + pixels[p + lines2]
                                                        + pixels[p + lines2 + 1]
                                                        + pixels[p + lines2 + 2]) / 5.0));
                    double left = Math.Abs(current - ((pixels[p - lines2 - 2]
                                                       + pixels[p - line1 - 2]
                                                       + pixels[p - 2]
                                                       + pixels[p + line1 - 2]
                                                       + pixels[p + lines2 - 2]) / 5.0));
                    double right = Math.Abs(current - ((pixels[p - lines2 + 2]
                                                        + pixels[p - line1 + 2]
                                                        + pixels[p + 2]
                                                        + pixels[p + line1 + 2]
                                                        + pixels[p + lines2 + 2]) / 5.0));
                    //4ラインとの差で一番小さい値
                    double v = Math.Min(upper, Math.Min(lower, Math.Min(left, right)));

                    //v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                    filtered[p] = (byte)Math.Abs(v);
                    //filtered[p] = (byte)v;
                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }

        /// <summary>
        /// ノイズ(エッジ)判定を元にぼかし
        /// </summary>
        /// <param name="isFilter">ノイズ判定結果</param>
        /// <param name="threshold">処理判定</param>
        /// <param name="isUpper">trueでしきい値以上をぼかし処理、falseで以下を処理</param>
        /// <param name="pixels">元画像</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private (byte[] pixels, BitmapSource bitmap) FilterAndFilter(byte[] isFilter, int threshold, bool isUpper, byte[] pixels, int width, int height)
        {
            int[][] weight = new int[][] {
                new int[] { 0, 1, 0 },
                new int[] { 1, 1, 1 },
                new int[] { 0, 1, 0 } };
            //int offset = 0;
            int div = 5;
            byte[] filtered = new byte[pixels.Length];
            int p;

            //上下左右1ピクセルは処理しない
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    double v = 0.0;
                    p = x + y * width;
                    //しきい値以上をぼかしか、しきい値以下をぼかし
                    if ((isUpper & isFilter[p] > threshold) | (isUpper == false & isFilter[p] < threshold))
                    {
                        int pp;
                        for (int a = 0; a < 3; a++)
                        {
                            for (int b = 0; b < 3; b++)
                            {
                                pp = (x + b - 1) + ((y + a - 1) * width);
                                v += pixels[pp] * weight[a][b];
                            }
                        }
                        v /= div;
                        v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                        filtered[p] = (byte)v;
                    }
                    //無処理
                    else { filtered[p] = pixels[p]; }

                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }

        private (byte[] pixels, BitmapSource bitmap) MyFilterガウシアン5x5(byte[] pixels, int width, int height)
        {
            int[][] weight = new int[][] {
                new int[] { 1,  4,  6, 4, 1 },
                new int[] { 4, 16, 24, 16, 4 },
                new int[] { 6, 24, 36, 24, 6 },
                new int[] { 4, 16, 24, 16, 4 },
                new int[] { 1, 4, 6, 4, 1 } };

            int div = 256;
            byte[] filtered = new byte[pixels.Length];
            int p;

            //外周2ピクセルラインは処理しない
            for (int y = 2; y < height - 2; y++)
            {
                for (int x = 2; x < width - 2; x++)
                {
                    double v = 0.0;
                    p = x + y * width;
                    int pp;
                    for (int a = 0; a < 5; a++)
                    {
                        for (int b = 0; b < 5; b++)
                        {
                            pp = (x + b - 2) + ((y + a - 2) * width);
                            v += pixels[pp] * weight[a][b];
                        }
                    }
                    v /= div;
                    v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                    filtered[p] = (byte)v;
                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }

        private (byte[] pixels, BitmapSource bitmap) MyFilterバイラテラルフィルタ失敗だけどかっこいい3x3(byte[] pixels, int width, int height)
        {
            int[][] weight = new int[][] {
                new int[] { 1, 2, 1 },
                new int[] { 2, 4, 2 },
                new int[] { 1, 2, 1 } };

            int div = 16;
            byte[] filtered = new byte[pixels.Length];
            int p;
            int diffBrightness;
            double weightBrightness;
            byte pValue;
            //外周1ピクセルラインは処理しない
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    double v = 0.0;
                    p = x + y * width;
                    pValue = pixels[p];
                    int pp;
                    for (int a = 0; a < 3; a++)
                    {
                        for (int b = 0; b < 3; b++)
                        {
                            pp = (x + b - 1) + ((y + a - 1) * width);
                            diffBrightness = 255 - Math.Abs(pValue - pixels[pp]);
                            weightBrightness = diffBrightness / 255;
                            v += pixels[pp] * weight[a][b] * weightBrightness;
                        }
                    }
                    v /= div;
                    v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                    filtered[p] = (byte)v;
                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }

        private (byte[] pixels, BitmapSource bitmap) MyFilterバイラテラルフィルタ3x3(byte[] pixels, int width, int height)
        {
            int[][] weight = new int[][] {
                new int[] { 1, 2, 1 },
                new int[] { 2, 4, 2 },
                new int[] { 1, 2, 1 } };

            int div = 16;
            byte[] filtered = new byte[pixels.Length];
            int p;
            double diffBrightness;
            double weightBrightness;
            byte pValue;
            //外周1ピクセルラインは処理しない
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    double v = 0.0;
                    p = x + y * width;
                    pValue = pixels[p];
                    int pp;
                    byte ppValue;
                    double nekoWeight;
                    for (int a = 0; a < 3; a++)
                    {
                        for (int b = 0; b < 3; b++)
                        {
                            pp = (x + b - 1) + ((y + a - 1) * width);
                            ppValue = pixels[pp];
                            diffBrightness = 255 - Math.Abs(pValue - ppValue);
                            weightBrightness = diffBrightness / 255;
                            nekoWeight = weight[a][b] * weightBrightness;
                            v += ppValue * weight[a][b] * weightBrightness;
                        }
                    }
                    v /= div;
                    v = (v > 255) ? 255 : (v < 0) ? 0 : v;
                    filtered[p] = (byte)v;
                }
            }

            return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        }






        //#region 未使用フィルタ

        //private (byte[] pixesl, BitmapSource bitmap) TestFilter(byte[] pixels, int width, int height, byte threshold)
        //{
        //    //ノイズ除去のつもりだったけどぼかし効果になった
        //    //自身と周辺8画素を比較して輝度が10以上離れていたら、周辺8画素の平均輝度に変更する
        //    //byte threshold = 10;//小さいほどぼやける
        //    byte[] filtered = new byte[pixels.Length];
        //    //めんどくさいので上下左右1ピクセルは処理しない
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        for (int x = 1; x < width - 1; x++)
        //        {
        //            int pp;
        //            List<byte> around = new List<byte>();
        //            for (int a = 0; a < 3; a++)
        //            {
        //                for (int b = 0; b < 3; b++)
        //                {
        //                    pp = (x + b - 1) + ((y + a - 1) * width);
        //                    around.Add(pixels[pp]);
        //                }
        //            }
        //            byte current = around[4];//自身の値
        //            around.RemoveAt(4);//自身の値を除去して平均値を求める
        //            double average = around.Average(z => z);
        //            if (Math.Abs(average - current) > threshold) { current = (byte)average; }
        //            filtered[x + y * width] = current;
        //        }
        //    }
        //    return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        //}

        //private (byte[] pixesl, BitmapSource bitmap) Filter上下左右(byte[] pixels, int width, int height, byte threshold)
        //{
        //    //上下左右の平均との差がしきい値以上ならノイズと判定して
        //    //平均値に変更する

        //    byte[] filtered = new byte[pixels.Length];
        //    //めんどくさいので上下左右1ピクセルは処理しない
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        for (int x = 1; x < width - 1; x++)
        //        {
        //            byte current = pixels[y * width + x];//自身の値
        //            double ave = 0;
        //            ave += pixels[(y - 1) * width + x];//top
        //            ave += pixels[(y + 1) * width + x];//bottom
        //            ave += pixels[y * width + x - 1];//left
        //            ave += pixels[y * width + x + 1];//right
        //            ave /= 4;
        //            //double average = around.Average(z => z);
        //            if (Math.Abs(ave - current) > threshold)
        //            {
        //                current = (byte)ave;
        //            }
        //            filtered[x + y * width] = current;
        //        }
        //    }
        //    return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        //}

        //private (byte[] pixesl, BitmapSource bitmap) TestFilter3(byte[] pixels, int width, int height)
        //{
        //    //イマイチ
        //    //ノイズ除去のつもりだったけどぼかし効果になった
        //    //自身と斜めの4画素を比較して輝度が閾値以上離れていたら、周辺4画素の平均輝度に変更する
        //    byte threshold = 100;//これ以上離れていたら平均値にする
        //    byte[] filtered = new byte[pixels.Length];
        //    //めんどくさいので上下左右1ピクセルは処理しない
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        for (int x = 1; x < width - 1; x++)
        //        {
        //            List<byte> around = new List<byte>();
        //            byte current = pixels[y * width + x];//自身の値
        //            around.Add(pixels[(y - 1) * width + x - 1]);//topLeft
        //            around.Add(pixels[(y + 1) * width + x - 1]);//bottomLeft
        //            around.Add(pixels[(y - 1) * width + x + 1]);//topRignt
        //            around.Add(pixels[(y + 1) * width + x + 1]);//bottomRight

        //            double average = around.Average(z => z);
        //            if (Math.Abs(average - current) > threshold)
        //            {
        //                current = (byte)average;
        //            }
        //            filtered[x + y * width] = current;
        //        }
        //    }
        //    return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        //}

        //private (byte[] pixesl, BitmapSource bitmap) TestFilter4(byte[] pixels, int width, int height)
        //{
        //    //かなりいまいち
        //    //ノイズ除去のつもりだったけどぼかし効果になった
        //    //自身と上下左右4画素を比較して輝度が10以上離れていたら、周辺4画素の平均輝度に変更する
        //    //上下左右どれかに自身と似た輝度があれば、そのままにする
        //    byte threshold = 10;//これ以上離れていたら平均値にする
        //    byte similar = 1;//似た輝度の閾値、これ以下なら似ている
        //    byte[] filtered = new byte[pixels.Length];
        //    //めんどくさいので上下左右1ピクセルは処理しない
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        for (int x = 1; x < width - 1; x++)
        //        {
        //            List<byte> around = new List<byte>();
        //            byte current = pixels[y * width + x];//自身の値
        //            around.Add(pixels[(y - 1) * width + x]);//top
        //            around.Add(pixels[(y + 1) * width + x]);//bottom
        //            around.Add(pixels[y * width + x - 1]);//left
        //            around.Add(pixels[y * width + x + 1]);//right
        //            //平均値からかけ離れていて
        //            double average = around.Average(z => z);
        //            double diff = Math.Abs(average - current);
        //            if (diff > threshold)
        //            {
        //                //上下左右のどれか
        //                if (similar < Math.Abs(around[0] - current) &&
        //                    similar < Math.Abs(around[1] - current) &&
        //                    similar < Math.Abs(around[2] - current) &&
        //                    similar < Math.Abs(around[3] - current))
        //                {
        //                    //平均値に変更
        //                    current = (byte)average;
        //                }
        //            }
        //            filtered[x + y * width] = current;
        //        }
        //    }
        //    return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        //}

        ////エッジ抽出後、メディアンフィルタ
        //private (byte[] pixels, BitmapSource bitmap) Filterラプラシアンとメディアン(byte[] pixels, int width, int height)
        //{
        //    //ラプラシアン、エッジ抽出
        //    int[][] weight = new int[][] {
        //        new int[] { 0, -1, 0 },
        //        new int[] { -1, 4, -1 },
        //        new int[] { 0, -1, 0 } };

        //    byte threshold = 100;//絶対値でこれ以上のエッジならメディアンフィルタで変更しない
        //    bool[] IsFilter = new bool[pixels.Length];//判定結果用
        //    int p;
        //    //めんどくさいので上下左右1ピクセルは処理しない
        //    //ラプラシアンでエッジ判定
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        for (int x = 1; x < width - 1; x++)
        //        {
        //            int edge = 0;
        //            p = x + y * width;
        //            int pp;
        //            for (int a = 0; a < 3; a++)
        //            {
        //                for (int b = 0; b < 3; b++)
        //                {
        //                    pp = (x + b - 1) + ((y + a - 1) * width);
        //                    edge += pixels[pp] * weight[a][b];
        //                }
        //            }
        //            //エッジ判定結果
        //            byte moto = pixels[p];
        //            int diff = Math.Abs(edge - moto);
        //            //IsFilter[p] = (Math.Abs(edge) < threshold) ? true : false;
        //            if (Math.Abs(edge) < threshold) { IsFilter[p] = true; }

        //            ////差が小さければメディアンフィルタする(true)
        //            //if (diff < threshold) { IsFilter[p] = true; }
        //            //else IsFilter[p] = false;
        //        }
        //    }
        //    //メディアンフィルタ
        //    byte[] filtered = new byte[pixels.Length];
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        for (int x = 1; x < width - 1; x++)
        //        {
        //            p = x + y * width;
        //            //エッジはパス
        //            if (IsFilter[p] == false)
        //            {
        //                filtered[p] = pixels[p];
        //                continue;//パス
        //            }

        //            int pp;
        //            List<byte> sort = new List<byte>();
        //            for (int a = 0; a < 3; a++)
        //            {
        //                for (int b = 0; b < 3; b++)
        //                {
        //                    pp = (x + b - 1) + ((y + a - 1) * width);
        //                    sort.Add(pixels[pp]);
        //                }
        //            }
        //            //ソートして中央値を採用
        //            var sorted = sort.OrderBy(z => z);
        //            filtered[p] = sorted.ToList()[4];
        //        }
        //    }
        //    return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        //}


        //private (byte[] pixesl, BitmapSource bitmap) Filter上下左右2(byte[] pixels, int width, int height, byte threshold)
        //{
        //    //上下左右の平均との差がしきい値以上ならノイズと判定して
        //    //平均値に変更するけど
        //    //左右の色と似ているか、上下の色が似ていたら変更しない、第2しきい値
        //    byte similer = 50;//これ以下なら変更なし
        //    byte[] filtered = new byte[pixels.Length];

        //    //めんどくさいので上下左右1ピクセルは処理しない
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        for (int x = 1; x < width - 1; x++)
        //        {

        //            byte current = pixels[y * width + x];//自身の値
        //            double ave = 0;
        //            int top = pixels[(y - 1) * width + x];//top
        //            int bottom = pixels[(y + 1) * width + x];//bottom
        //            int left = pixels[y * width + x - 1];//left
        //            int right = pixels[y * width + x + 1];//right

        //            int dTop = Math.Abs(current - top);
        //            int dBottom = Math.Abs(current - bottom);
        //            int dLeft = Math.Abs(current - left);
        //            int dRight = Math.Abs(current - right);
        //            if ((dTop < similer & dBottom < similer) | (dLeft < similer & dRight < similer))
        //            {
        //            }
        //            else if (Math.Abs(ave - current) < threshold)
        //            {
        //                current = (byte)((top + bottom + left + right) / 4);
        //            }
        //            filtered[x + y * width] = current;
        //            if (top == bottom) { }
        //        }
        //    }
        //    return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        //}

        ////エッジ抽出後、上下左右フィルタ
        //private (byte[] pixels, BitmapSource bitmap) Filterラプラシアンと上下左右(byte[] pixels, int width, int height, int threshold)
        //{
        //    //ラプラシアン、エッジ抽出
        //    int[][] weight = new int[][] {
        //        new int[] { 0, -1, 0 },
        //        new int[] { -1, 4, -1 },
        //        new int[] { 0, -1, 0 } };

        //    bool[] IsFilter = new bool[pixels.Length];//判定結果用
        //    int p;
        //    //めんどくさいので上下左右1ピクセルは処理しない
        //    //ラプラシアンでエッジ判定
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        for (int x = 1; x < width - 1; x++)
        //        {
        //            int edge = 0;
        //            p = x + y * width;
        //            int pp;
        //            for (int a = 0; a < 3; a++)
        //            {
        //                for (int b = 0; b < 3; b++)
        //                {
        //                    pp = (x + b - 1) + ((y + a - 1) * width);
        //                    edge += pixels[pp] * weight[a][b];
        //                }
        //            }
        //            //エッジ判定結果
        //            byte moto = pixels[p];
        //            int diff = Math.Abs(edge - moto);
        //            //IsFilter[p] = (Math.Abs(edge) < threshold) ? true : false;
        //            if (Math.Abs(edge) < threshold) { IsFilter[p] = true; }

        //            ////差が小さければフィルタする(true)
        //            //if (diff < threshold) { IsFilter[p] = true; }
        //            //else IsFilter[p] = false;
        //        }
        //    }

        //    var neko = IsFilter.Count(zz => zz == false);
        //    byte[] filtered = new byte[pixels.Length];
        //    //めんどくさいので上下左右1ピクセルは処理しない
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        for (int x = 1; x < width - 1; x++)
        //        {
        //            p = x + (y * width);
        //            //エッジはパス
        //            if (IsFilter[p] == false)
        //            {
        //                filtered[p] = pixels[p];
        //                continue;//パス
        //            }

        //            //ノイズなら平均値に変更
        //            byte current = pixels[p];//自身の値                    
        //            int top = pixels[(y - 1) * width + x];//top
        //            int bottom = pixels[(y + 1) * width + x];//bottom
        //            int left = pixels[p - 1];//left
        //            int right = pixels[p + 1];//right
        //            double ave = (top + bottom + left + right) / 4;
        //            double diff = Math.Abs(current - ave);

        //            if (Math.Abs(ave - current) > 1)
        //            {
        //                current = (byte)ave;
        //            }
        //            filtered[p] = current;
        //        }
        //    }
        //    return (filtered, BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, filtered, width));
        //}

        //#endregion 未使用フィルタ



        #region その他


        private void Button_Click_30(object sender, RoutedEventArgs e)
        {
            (byte[] pixels, BitmapSource bitmap) = ToReduceColor(MyPixels, 3, MyBitmap.PixelWidth, MyBitmap.PixelHeight, MyBitmap.PixelWidth);
            MyPixels = pixels;
            MyPixels膨張収縮 = pixels;
            MyImage.Source = bitmap;
        }
        private (byte[] pixels, BitmapSource bitmap) ToReduceColor(byte[] pixels, int bit, int width, int height, int stride)
        {
            var pixelsNew = new byte[pixels.Length];
            var table = new byte[256];
            double step = 255.0 / (Math.Pow(2, bit) - 1);//1階調ぶんの値
            int shift = 8 - bit;
            for (int i = 0; i < 256; i++)
            {
                table[i] = (byte)((i >> shift) * step);
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                pixelsNew[i] = table[pixels[i]];
            }

            //SetPalette(ColorBit);
            //SetPalette2(ColorBit);

            var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixelsNew, stride);
            return (pixelsNew, source);

        }

        private void Button_Click_21(object sender, RoutedEventArgs e)
        {
            BitmapSource source = (BitmapSource)MyImage.Source;
            //SaveImage(new FormatConvertedBitmap(source, PixelFormats.Indexed4, new BitmapPalette(source, 16), 0));
            //SaveImage(new FormatConvertedBitmap(source, PixelFormats.Indexed4, null, 0));
            SaveImage((BitmapSource)MyImage.Source);
        }

        private void SaveImage(BitmapSource source)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "*.png|*.png|*.bmp|*.bmp|*.tiff|*.tiff";
            saveFileDialog.AddExtension = true;
            saveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(ImageFileFullPath) + "_";
            saveFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(ImageFileFullPath);
            if (saveFileDialog.ShowDialog() == true)
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                if (saveFileDialog.FilterIndex == 1)
                {
                    encoder = new PngBitmapEncoder();
                }
                else if (saveFileDialog.FilterIndex == 2)
                {
                    encoder = new BmpBitmapEncoder();
                }
                else if (saveFileDialog.FilterIndex == 3)
                {
                    encoder = new TiffBitmapEncoder();
                }
                encoder.Frames.Add(BitmapFrame.Create(source));

                using (var fs = new System.IO.FileStream(saveFileDialog.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    encoder.Save(fs);
                }
            }
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false) { return; }
            string[] filePath = (string[])e.Data.GetData(DataFormats.FileDrop);
            var (pixels, bitmap) = MakeBitmapSourceAndByteArray(filePath[0], PixelFormats.Gray8, 96, 96);

            if (bitmap == null)
            {
                MessageBox.Show("画像ファイルじゃないみたい");
            }
            else
            {

                MyPixels = pixels;
                MyBitmap = bitmap;
                MyImage.Source = MyBitmap;
                MyImageOrigin.Source = MyBitmap;
                MyPixelsOrigin = MyPixels;
                MyEdge = MyPixels;
                MyPixels膨張収縮 = MyPixels;
            }
        }

        /// <summary>
        /// 画像ファイルからbitmapと、そのbyte配列を取得、ピクセルフォーマットを指定したものに変換
        /// </summary>
        /// <param name="filePath">画像ファイルのフルパス</param>
        /// <param name="pixelFormat">PixelFormatsを指定</param>
        /// <param name="dpiX">96が基本、指定なしなら元画像と同じにする</param>
        /// <param name="dpiY">96が基本、指定なしなら元画像と同じにする</param>
        /// <returns></returns>
        private (byte[] array, BitmapSource source) MakeBitmapSourceAndByteArray(string filePath, PixelFormat pixelFormat, double dpiX = 0, double dpiY = 0)
        {
            byte[] pixels = null;
            BitmapSource source = null;
            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    var bf = BitmapFrame.Create(fs);

                    var convertedBitmap = new FormatConvertedBitmap(bf, pixelFormat, null, 0);
                    int w = convertedBitmap.PixelWidth;
                    int h = convertedBitmap.PixelHeight;
                    int stride = (w * pixelFormat.BitsPerPixel + 7) / 8;
                    pixels = new byte[h * stride];
                    convertedBitmap.CopyPixels(pixels, stride, 0);
                    //dpi指定がなければ元の画像と同じdpiにする
                    if (dpiX == 0) { dpiX = bf.DpiX; }
                    if (dpiY == 0) { dpiY = bf.DpiY; }
                    //dpiを指定してBitmapSource作成
                    source = BitmapSource.Create(
                        w, h, dpiX, dpiY,
                        convertedBitmap.Format,
                        convertedBitmap.Palette, pixels, stride);
                };
            }
            catch (Exception)
            {
            }
            return (pixels, source);
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int aa = Panel.GetZIndex(MyImage);
            Panel.SetZIndex(MyImageOrigin, aa + 1);
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int aa = Panel.GetZIndex(MyImage);
            Panel.SetZIndex(MyImageOrigin, aa - 1);
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            //リセット
            MyImage.Source = MyImageOrigin.Source;
            MyPixels = MyPixelsOrigin;
            MyPixels膨張収縮 = MyPixelsOrigin;
            //MyPixelsBackup = MyPixelsOrigin;
        }

        #endregion

        private void MyButton1_Click(object sender, RoutedEventArgs e)
        {
            int[][] weight = new int[][] {
                new int[] { 0, 1, 0 },
                new int[] { 1, 1, 1 },
                new int[] { 0, 1, 0 } };
            int offset = 0;
            int div = 5;
            (byte[] pixels, BitmapSource bitmap) = Filter(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
            //MyPixelsBackup = pixels;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //ラプラシアン、エッジ抽出
            int[][] weight = new int[][] {
                new int[] { 0, -1, 0 },
                new int[] { -1, 4, -1 },
                new int[] { 0, -1, 0 } };
            int offset = 0;
            int div = 1;
            (byte[] pixels, BitmapSource bitmap) = Filter(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight,
                weight, div, offset, true);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //シャープネス
            int[][] weight = new int[][] {
                new int[] { 0, -1, 0 },
                new int[] { -1, 5, -1 },
                new int[] { 0, -1, 0 } };
            int offset = 0;
            int div = 1;
            (byte[] pixels, BitmapSource bitmap) = Filter(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset);
            //(byte[] pixels, BitmapSource bitmap) = Filter(MyPixels膨張収縮, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset);
            MyImage.Source = bitmap;
            //MyPixels膨張収縮 = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //エンボス
            int[][] weight = new int[][] {
                new int[] { -1, 0, 0 },
                new int[] { 0, 1, 0 },
                new int[] { 0, 0, 0 } };
            int offset = 128;
            int div = 1;
            (byte[] pixels, BitmapSource bitmap) = Filter(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight,
                weight, div, offset);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            //メディアン、ノイズ除去
            (byte[] pixels, BitmapSource bitmap) = MedianFilter(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            int[][] weight = new int[][] {
                new int[] { 0, 1, 0 },
                new int[] { 1, 1, 1 },
                new int[] { 0, 1, 0 } };
            int offset = 0;
            int div = 5;
            (byte[] pixels, BitmapSource bitmap) = Filter2(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            //ソーベルフィルタ
            (byte[] pixels, BitmapSource bitmap) = SobelFilterソーベルフィルタ(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            //ソーベルフィルタ横
            (byte[] pixels, BitmapSource bitmap) = SobelFilterソーベルフィルタ横(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            //ソーベルフィルタ縦
            (byte[] pixels, BitmapSource bitmap) = SobelFilterソーベルフィルタ縦(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            //ソーベルフィルタ2乗和の平方根
            (byte[] pixels, BitmapSource bitmap) = SobelFilterソーベルフィルタ2乗和の平方根(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            //ソーベルフィルタ斜め和
            (byte[] pixels, BitmapSource bitmap) = SobelFilterソーベルフィルタ斜め和(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            //ソーベルフィルタ斜め2乗和の平方根
            (byte[] pixels, BitmapSource bitmap) = SobelFilterソーベルフィルタ斜め2乗和の平方根(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            //一次微分フィルタ和
            (byte[] pixels, BitmapSource bitmap) = EdgeFilter一次微分フィルタ和(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_13(object sender, RoutedEventArgs e)
        {
            //一次微分フィルタ2乗和の平方根
            (byte[] pixels, BitmapSource bitmap) = EdgeFilter一次微分フィルタ2乗和の平方根(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_14(object sender, RoutedEventArgs e)
        {
            //プレウィットフィルタ和
            (byte[] pixels, BitmapSource bitmap) = EdgeFilterプレウィットフィルタ和(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_15(object sender, RoutedEventArgs e)
        {
            //プレウィットフィルタ2乗和の平方根
            (byte[] pixels, BitmapSource bitmap) = EdgeFilterプレウィットフィルタ2乗和の平方根(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_16(object sender, RoutedEventArgs e)
        {
            //ロバーツフィルタ和
            (byte[] pixels, BitmapSource bitmap) = EdgeFilterロバーツフィルタ和(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_17(object sender, RoutedEventArgs e)
        {
            //ロバーツフィルタ2乗和の平方根
            (byte[] pixels, BitmapSource bitmap) = EdgeFilterロバーツフィルタ2乗和の平方根(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_18(object sender, RoutedEventArgs e)
        {
            //ラプラシアン近傍8
            int[][] weight = new int[][] {
                new int[] { 1, 1, 1 },
                new int[] { 1, -8, 1 },
                new int[] { 1, 1, 1 } };
            int offset = 0;
            int div = 1;
            (byte[] pixels, BitmapSource bitmap) = Filter(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset, true);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_19(object sender, RoutedEventArgs e)
        {
            //膨張
            (byte[] pixels, BitmapSource bitmap) = ScaleFilter膨張(MyPixels膨張収縮, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            MyPixels膨張収縮 = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_20(object sender, RoutedEventArgs e)
        {
            //収縮
            (byte[] pixels, BitmapSource bitmap) = ScaleFilter収縮(MyPixels膨張収縮, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            MyPixels膨張収縮 = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_22(object sender, RoutedEventArgs e)
        {
            //
            int[][] weight = new int[][] {
                new int[] { -1, 2, -1 },
                new int[] { 2, -4, 2 },
                new int[] { -1, 2, -1 } };
            int offset = 0;
            int div = 1;
            (byte[] pixels, BitmapSource bitmap) = Filter(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_23(object sender, RoutedEventArgs e)
        {
            (byte[] pixels, BitmapSource bitmap) = EdgeFilter一次微分フィルタ和(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            //(byte[] pixels, BitmapSource bitmap) = EdgeFilterプレウィットフィルタ和(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            //(byte[] pixels, BitmapSource bitmap) = SobelFilterソーベルフィルタ(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            ////ラプラシアン、エッジ抽出
            int[][] weight = new int[][] {
                new int[] { 0, -1, 0 },
                new int[] { -1, 4, -1 },
                new int[] { 0, -1, 0 } };
            int offset = 0;
            int div = 1;
            //(byte[] pixels, BitmapSource bitmap) = Filter(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset);
            //ぼかし
            weight = new int[][] {
                new int[] { 0, 1, 0 },
                new int[] { 1, 1, 1 },
                new int[] { 0, 1, 0 } };
            offset = 0;
            div = 5;
            (pixels, bitmap) = FilterAndFilter(pixels, 150, MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_24(object sender, RoutedEventArgs e)
        {
            //            ラプラシアン・フィルタをVerilog HDLで実装する -メモ置き場
            //https://okchan08.hateblo.jp/entry/2019/01/21/120000

            //ラプラシアン、エッジ抽出
            int[][] weight = new int[][] {
                new int[] { -1, -3, -4, -3, -1 },
                new int[] { -3,  0,  6,  0, -3 },
                new int[] { -4,  6, 20,  6, -4 },
                new int[] { -3,  0,  6,  0, -1 },
                new int[] { -1, -3, -4, -3, -1 } };
            int offset = 0;
            int div = 1;
            (byte[] pixels, BitmapSource bitmap) = Filter5x5(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_25(object sender, RoutedEventArgs e)
        {
            //2ライン右との差
            (byte[] pixels, BitmapSource bitmap) = MyFilterHorizontal(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_26(object sender, RoutedEventArgs e)
        {
            //2ライン下とだけの差
            (byte[] pixels, BitmapSource bitmap) = MyFilterVertical(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;
        }

        private void Button_Click_27(object sender, RoutedEventArgs e)
        {
            (byte[] pixels, BitmapSource bitmap) = MyFilter5x5エッジ抽出1マス先(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            //MyPixels = pixels;
            MyEdge = pixels;

        }

        private void Button_Click_28(object sender, RoutedEventArgs e)
        {
            if (MyEdge == null) { return; }
            //しきい値以上のノイズ(エッジ)判定を元にぼかし
            //エッジだけをぼかし
            (byte[] pixels, BitmapSource bitmap) = FilterAndFilter(MyEdge, (int)MyThreshold.Value, true, MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            MyPixels = pixels;
            MyPixels膨張収縮 = pixels;
            //MyEdge = pixels;
        }

        private void Button_Click_29(object sender, RoutedEventArgs e)
        {
            if (MyEdge == null) { return; }
            //しきい値以上のノイズ(エッジ)判定を元にぼかし
            //エッジ以外をぼかし
            (byte[] pixels, BitmapSource bitmap) = FilterAndFilter(MyEdge, (int)MyThreshold.Value, false, MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            MyPixels = pixels;
            MyPixels膨張収縮 = pixels;
            //MyEdge = pixels;
        }

        private void Button_Click_31(object sender, RoutedEventArgs e)
        {
            //ガウシアン5x5
            int[][] weight = new int[][] {
                new int[] { 1,  4,  6, 4, 1 },
                new int[] { 4, 16, 24, 16, 4 },
                new int[] { 6, 24, 36, 24, 6 },
                new int[] { 4, 16, 24, 16, 4 },
                new int[] { 1, 4, 6, 4, 1 } };
            int div = 256;
            int offset = 0;
            (byte[] pixels, BitmapSource bitmap) = Filter5x5(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, offset);
            MyImage.Source = bitmap;
            MyEdge = pixels;

        }

        private void Button_Click_32(object sender, RoutedEventArgs e)
        {
            //ガウシアン3x3
            int[][] weight = new int[][] {
                new int[] { 1, 2, 1 },
                new int[] { 2, 4, 2 },
                new int[] { 1, 2, 1 } };
            int div = 16;
            (byte[] pixels, BitmapSource bitmap) = Filter(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight, weight, div, 0);
            MyImage.Source = bitmap;
            MyEdge = pixels;
        }

        private void Button_Click_33(object sender, RoutedEventArgs e)
        {
            (byte[] pixels, BitmapSource bitmap) = MyFilterバイラテラルフィルタ失敗だけどかっこいい3x3(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            MyEdge = pixels;

        }

        private void Button_Click_34(object sender, RoutedEventArgs e)
        {
            (byte[] pixels, BitmapSource bitmap) = MyFilterバイラテラルフィルタ3x3(MyPixels, MyBitmap.PixelWidth, MyBitmap.PixelHeight);
            MyImage.Source = bitmap;
            MyEdge = pixels;

        }
    }
}
