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
using MyHSV;
//画像の色相の状態をレーダーチャートふうに表示してみた(ソフトウェア ) - 午後わてんのブログ - Yahoo!ブログ
//https://blogs.yahoo.co.jp/gogowaten/15920156.html

namespace _20190328_色相ヒストグラム
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            //レーダーチャート型に切り抜き
            //MyTest();//PCのファイル画像から
            MyTest2();//アプリに埋め込んだ画像から
        }

        //アプリに埋め込んだ画像からレーダーチャート
        private void MyTest2()
        {
            string fileName = "";
            fileName = "_20190328_色相ヒストグラム.HSVRectValue.png";
            //fileName = "_20190328_色相ヒストグラム.NEC_1456_2018_03_17_午後わてん.jpg";
            //fileName = "_20190328_色相ヒストグラム.NEC_6221_2019_02_24_午後わてん_16colors.png";
            //fileName = "_20190328_色相ヒストグラム.NEC_6418_2019_03_27_午後わてん.jpg";
            //fileName = "_20190328_色相ヒストグラム.青空とトマトの花.jpg";


            double radius = 200.0;
            Point center = new Point(radius, radius);

            //色相の四角形画像作成表示
            MyImage1.Source = MakeHueRountRect((int)radius * 2, (int)radius * 2);

            byte[] pixels;
            BitmapSource bitmap;
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.IO.Stream stream = assembly.GetManifestResourceStream(fileName);
            (pixels, bitmap) = MakeBitmapSourceAndByteArray(stream, PixelFormats.Bgra32, 96, 96);

            MyImage1.Clip = MakeClipPathGeometry(MakeClipPoints(pixels, 360, radius, center));
            MyImage2.Source = bitmap;
        }

        //PCにある画像ファイルからレーダーチャート
        private void MyTest()
        {
            string filePath;
            filePath = @"D:\ブログ用\チェック用2\NEC_6418_2019_03_27_午後わてん.jpg";
            //filePath = @"D:\ブログ用\チェック用2\NEC_6221_2019_02_24_午後わてん_16colors.png";
            //filePath = @"D:\ブログ用\テスト用画像\青空とトマトの花.jpg";
            //filePath = @"D:\ブログ用\テスト用画像\NEC_1456_2018_03_17_午後わてん.jpg";
            filePath = @"D:\ブログ用\テスト用画像\HSVRectValue.png";
            filePath = @"D:\ブログ用\チェック用2\NEC_6459_2019_03_31_午後わてん.jpg";
            //画像ファイルからピクセルの色の配列とBitmapを取得
            (byte[] pixels, BitmapSource bitmap) aa = MakeBitmapSourceAndByteArray(filePath, PixelFormats.Bgra32, 96, 96);

            double radius = 200.0;
            Point center = new Point(radius, radius);

            //色相の四角形画像作成表示
            MyImage1.Source = MakeHueRountRect((int)radius * 2, (int)radius * 2);

            //Histogram360(aa.array);//360分割
            //MyImage1.Clip = MakeClipPathGeometry(MakeClip6Segment(aa.array));//6分割
            //任意分割
            
            MyImage1.Clip = MakeClipPathGeometry(MakeClipPoints(aa.pixels, 60, radius, center));
            MyImage2.Source = aa.bitmap;
        }

        /// <summary>
        /// PointのリストからPathGeometry作成
        /// </summary>
        /// <param name="pc"></param>
        /// <returns></returns>
        private PathGeometry MakeClipPathGeometry(List<Point> pc)
        {
            var fig = new PathFigure();
            fig.StartPoint = pc[0];
            //pc.RemoveAt(0);//これはいらないかも
            PolyLineSegment segment = new PolyLineSegment();
            segment.Points = new PointCollection(pc);
            fig.Segments.Add(segment);
            fig.IsClosed = true;//Pathを閉じる
            var pg = new PathGeometry();
            //塗りつぶしのルール、Pathが交差したときに残すか切り抜くかの違い？
            pg.FillRule = FillRule.Nonzero;//これはいらないかも
            //var neko = pg.FillRule;//初期値はevenodd
            pg.Figures.Add(fig);
            return pg;
        }



        /// <summary>
        /// 色相の分割範囲ごとのピクセル数をカウント、
        /// 分割数divCountが4なら、360/4＝90度毎、範囲0(315~45)、範囲1(45~135)、範囲2(135~225)、範囲3(225~315)
        /// </summary>
        /// <param name="pixels">PixelFormats.Bgra32のbyte配列</param>
        /// <param name="divCount">3～360で指定、色相分割数</param>
        /// <returns></returns>
        private int[] HuePixelCount(byte[] pixels, int divCount)
        {
            int[] table = new int[divCount];
            double div = 360.0 / divCount;
            double divdiv = div / 2.0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                //ピクセルの色相取得
                double hue = HSV.Color2HSV(pixels[i + 2], pixels[i + 1], pixels[i]).Hue;
                if (hue == 360.0) { continue; }//色相360は無彩色なのでパス

                //色相の範囲ごとにカウント
                hue = Math.Floor((hue + divdiv) / div);
                hue = (hue >= divCount) ? 0 : hue;
                table[(int)hue]++;
            }
            return table;
        }


        /// <summary>
        /// 切り抜き用のPointリスト作成、レーダーチャートの要素の値にあたる
        /// </summary>
        /// <param name="pixels">PixelFormats.Bgra32のbyte配列</param>
        /// <param name="divCount">色相の分割数、3～360で指定</param>
        /// <param name="radius">レーダーチャート画像の辺の半分(半径)を指定</param>
        /// <param name="center">中心点座標</param>
        /// <returns></returns>
        private List<Point> MakeClipPoints(byte[] pixels, int divCount, double radius, Point center)
        {
            //色相範囲ごとのピクセル数取得
            //配列の(Index * 360 / divCount)が色相で、値がピクセル数になる            
            int[] table = HuePixelCount(pixels, divCount);
            //最大値は相対的なレーダーチャートの半径にする
            double max = table.Max();

            double distance, radian, x, y;
            double div = 360.0 / divCount;
            var pc = new List<Point>();
            for (int i = 0; i < table.Length; i++)
            {
                radian = Radian(i * div);//色相(角度)をラジアンに変換
                //中心からの距離
                distance = table[i] / max;//最大値を1としたときの今の値
                distance *= radius;//*レーダーチャート画像の半径
                //中心からの位置
                x = Math.Cos(radian) * distance + center.X;
                y = Math.Sin(radian) * distance + center.Y;
                pc.Add(new Point(x, y));
            }
            return pc;
        }

        /// <summary>
        /// 切り抜き用のPointリスト作成、レーダーチャートの要素の値にあたる
        /// </summary>
        /// <param name="pixels">PixelFormats.Bgra32のbyte配列</param>
        /// <param name="divCount">色相の分割数、3～360で指定</param>
        /// <param name="radius">レーダーチャート画像の辺の半分(半径)を指定</param>
        /// <returns></returns>
        private List<Point> MakeClipPoints(byte[] pixels, int divCount, int radius)
        {
            //色相範囲ごとのピクセル数取得
            //配列の(Index * 360 / divCount)が色相で、値がピクセル数になる            
            int[] table = HuePixelCount(pixels, divCount);
            //最大値は相対的なレーダーチャートの半径にする
            double max = table.Max();

            double distance, radian, x, y;
            double div = 360.0 / divCount;
            var pc = new List<Point>();
            for (int i = 0; i < table.Length; i++)
            {
                radian = Radian(i * div);//色相(角度)をラジアンに変換
                //中心からの距離
                distance = table[i] / max;//最大値を1としたときの今の値
                distance *= radius;//*レーダーチャート画像の半径
                //中心からの位置
                x = Math.Cos(radian) * distance + radius;
                y = Math.Sin(radian) * distance + radius;
                pc.Add(new Point(x, y));
            }
            return pc;
        }



        private List<Point> MakeClip6Segment(byte[] pixels)
        {
            var table = Hue6Count(pixels);
            double max = table.Max();
            double distance, radian, x, y;
            var pc = new List<Point>();
            for (int i = 0; i < table.Length; i++)
            {
                radian = Radian(i * 60);
                distance = table[i] / max;
                distance *= 100;
                x = Math.Cos(radian) * distance + 100;
                y = Math.Sin(radian) * distance + 100;
                pc.Add(new Point(x, y));
            }
            return pc;
        }


        private void Histogram360(byte[] pixels)
        {
            //360度毎のピクセル数を取得
            var table = Hue360Count(pixels);
            table[360] = 0;//色相360は無彩色なので0個にする

            //最大ピクセル数を持つ色相(角度)をは半径の最大値にする
            int max = table.Max(x => x.Value);
            var sorted = table.OrderBy(x => x.Key);

            double distance;
            double radian;
            var pc = new List<Point>();
            for (int i = 0; i < 360; i++)
            {
                distance = 0;
                radian = Radian(i);
                if (table.ContainsKey(i) == true)
                {
                    distance = table[i];
                    distance /= max;
                    distance *= 100;
                }
                var x = Math.Cos(radian) * distance + 100;
                var y = Math.Sin(radian) * distance + 100;
                pc.Add(new Point(x, y));
            }
            PolyLineSegment segment = new PolyLineSegment();
            segment.Points = new PointCollection(pc);
            var fig = new PathFigure();
            fig.Segments.Add(segment);
            fig.StartPoint = pc[0];
            fig.IsClosed = true;
            var pg = new PathGeometry();
            pg.FillRule = FillRule.Nonzero;//これはいらないかも
            //var neko = pg.FillRule;//初期値はevenodd
            pg.Figures.Add(fig);
            MyImage1.Clip = pg;
        }

        //360度ごとのピクセル数をカウント
        private Dictionary<int, int> Hue360Count(byte[] pixels)
        {
            var table = new Dictionary<int, int>();//Hue,Count
            int hue;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                //var ihsv = HSV.Color2HSV(pixels[i + 2], pixels[i + 1], pixels[i]);
                //var dhue = ihsv.Hue;
                //int inthue = (int)Math.Round(dhue, MidpointRounding.AwayFromZero);
                hue = (int)Math.Round(HSV.Color2HSV(pixels[i + 2], pixels[i + 1], pixels[i]).Hue, MidpointRounding.AwayFromZero);
                if (table.ContainsKey(hue) == false)
                {
                    table.Add(hue, 1);
                }
                else { table[hue]++; }
            }
            return table;
        }


        //360度を6分割した範囲ごとのピクセル数をカウント
        private int[] Hue6Count(byte[] pixels)
        {
            int[] table = new int[6];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                double ihsv = HSV.Color2HSV(pixels[i + 2], pixels[i + 1], pixels[i]).Hue;
                if (ihsv == 360.0) { continue; }//色相360は無彩色なのでパス
                var aa = ihsv / 60.0;
                var bb = (int)Math.Round(aa, MidpointRounding.AwayFromZero);
                if (bb == 6) { bb = 0; }
                table[bb]++;
            }
            return table;
        }


        /// <summary>
        /// pixelsFormats.Rgb24の色相環作成用のBitmap作成
        /// 右が赤、時計回り
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private BitmapSource MakeHueRountRect(int width, int height)
        {
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            //色情報用のバイト配列作成
            int stride = wb.BackBufferStride;//横一列のバイト数、24bit = 8byteに横ピクセル数をかけた値
            byte[] pixels = new byte[height * stride * 8];//*8はbyteをbitにするから

            //100ｘ100のとき中心は50，50
            //ピクセル位置と画像の中心との差
            int xDiff = width / 2;
            int yDiff = height / 2;
            int p = 0;//今のピクセル位置の配列での位置
            for (int y = 0; y < height; y++)//y座標
            {
                for (int x = 0; x < width; x++)//x座標
                {
                    //今の位置の角度を取得、これが色相になる
                    double radian = Math.Atan2(y - yDiff, x - xDiff);
                    double kakudo = Degrees(radian);
                    //色相をColorに変換
                    Color c = HSV.HSV2Color(kakudo, 1.0, 1.0);
                    //バイト配列に色情報を書き込み
                    p = y * stride + x * 3;
                    pixels[p] = c.R;
                    pixels[p + 1] = c.G;
                    pixels[p + 2] = c.B;
                }
            }
            //バイト配列をBitmapに書き込み
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return wb;
        }


        //ラジアンを0～360の角度に変換
        public double Degrees(double radian)
        {
            double deg = radian / Math.PI * 180;
            if (deg < 0) deg += 360;
            return deg;
        }

        //角度をラジアンに変換
        private double Radian(double degree)
        {
            return Math.PI / 180.0 * degree;
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

        private (byte[] array, BitmapSource source) MakeBitmapSourceAndByteArray(System.IO.Stream stream, PixelFormat pixelFormat, double dpiX = 0, double dpiY = 0)
        {
            byte[] pixels = null;
            BitmapSource source = null;
            try
            {
                var bf = BitmapFrame.Create(stream);
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

            }
            catch (Exception)
            {
            }
            return (pixels, source);
        }
    }
}
