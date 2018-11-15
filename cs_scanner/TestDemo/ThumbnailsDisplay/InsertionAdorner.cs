//==========================================================================
//   Copyright(C) 2010,AGRICULTURAL BANK OF CHINA,Corp.All rights reserved 
//==========================================================================
//--------------------------------------------------------------------------
//程序名: InsertionAdorner
//功能: 插入位置标识类
//作者: qizhenguo
//创建日期: 2011-3-31
//--------------------------------------------------------------------------
//修改历史:
//日期      修改人     修改
//2011-3-31 qizhenguo  创建代码
//--------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;

namespace UFileClient.Display
{
    /// <summary>
    /// 插入位置标识类
    /// </summary>
    public class InsertionAdorner : Adorner
    {
        #region 私有变量
        private bool isSeparatorHorizontal;
        public bool IsInFirstHalf { get; set; }
        private AdornerLayer adornerLayer;
        private static Pen pen;
        private static PathGeometry triangle;
        #endregion

        #region 构造函数
        /// <summary>
        ///  静态构造函数，生成画笔和标识框
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        static InsertionAdorner()
        {
            pen = new Pen { Brush = Brushes.Black, Thickness = 5 };
            pen.Freeze();

            LineSegment firstLine = new LineSegment(new Point(0, -5), false);
            firstLine.Freeze();
            LineSegment secondLine = new LineSegment(new Point(0, 5), false);
            secondLine.Freeze();

            PathFigure figure = new PathFigure { StartPoint = new Point(5, 0) };
            figure.Segments.Add(firstLine);
            figure.Segments.Add(secondLine);
            figure.Freeze();

            triangle = new PathGeometry();
            triangle.Figures.Add(figure);
            triangle.Freeze();
        }

        /// <summary>
        ///  构造函数
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public InsertionAdorner(bool isSeparatorHorizontal, bool isInFirstHalf, UIElement adornedElement, AdornerLayer adornerLayer)
            : base(adornedElement)
        {
            this.isSeparatorHorizontal = isSeparatorHorizontal;
            this.IsInFirstHalf = isInFirstHalf;
            this.adornerLayer = adornerLayer;
            this.IsHitTestVisible = false;

            this.adornerLayer.Add(this);
        }
        #endregion

        #region 内部方法
        /// <summary>
        ///     描绘插入标识
        /// <param name="drawingContext"></param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        protected override void OnRender(DrawingContext drawingContext)
        {
            Point startPoint;
            Point endPoint;

            CalculateStartAndEndPoint(out startPoint, out endPoint);
            drawingContext.DrawLine(pen, startPoint, endPoint);

            if (this.isSeparatorHorizontal)
            {
                DrawTriangle(drawingContext, startPoint, 0);
                DrawTriangle(drawingContext, endPoint, 180);
            }
            else
            {
                DrawTriangle(drawingContext, startPoint, 90);
                DrawTriangle(drawingContext, endPoint, -90);
            }
        }

        /// <summary>
        ///     描绘插入标识框
        /// <param name="drawingContext">DrawingContext类</param>
        /// <param name="origin">起始点</param>
        /// <param name="angle">角度</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        private void DrawTriangle(DrawingContext drawingContext, Point origin, double angle)
        {
            drawingContext.PushTransform(new TranslateTransform(origin.X, origin.Y));
            drawingContext.PushTransform(new RotateTransform(angle));

            drawingContext.DrawGeometry(pen.Brush, null, triangle);

            drawingContext.Pop();
            drawingContext.Pop();
        }

        /// <summary>
        ///     计算开始结束点
        /// <out name="startPoint">起始点</param>
        /// <out name="endPoint">结束点</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        private void CalculateStartAndEndPoint(out Point startPoint, out Point endPoint)
        {
            startPoint = new Point();
            endPoint = new Point();

            double width = this.AdornedElement.RenderSize.Width;
            double height = this.AdornedElement.RenderSize.Height;

            if (this.isSeparatorHorizontal)
            {
                endPoint.X = width;
                if (!this.IsInFirstHalf)
                {
                    startPoint.Y = height;
                    endPoint.Y = height;
                }
            }
            else
            {
                endPoint.Y = height;
                if (!this.IsInFirstHalf)
                {
                    startPoint.X = width;
                    endPoint.X = width;
                }
            }
        }
        #endregion

        #region 共有方法
        /// <summary>
        ///     移除插入位置标识
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        public void Detach()
        {
            this.adornerLayer.Remove(this);
        }
        #endregion

    }
}
