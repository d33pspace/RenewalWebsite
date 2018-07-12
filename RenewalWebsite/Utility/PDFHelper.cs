﻿using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Utility
{
    public class PDFHelper : PdfPageEventHelper
    {
        // This is the contentbyte object of the writer
        iTextSharp.text.pdf.PdfContentByte cb;

        public string startDate;
        public string endDate;
        public string fullName;
        public string logoPath;
        public bool isAdd;
        public string sealImagePath;
        public string RenewalHeader;
        public string recordHeader;
        public string To;
        public string language;
        public string fontPath;
        public string boldFontPath;

        // this is the BaseFont we are going to use for the header / footer
        BaseFont bf = null;

        // This keeps track of the creation time
        DateTime PrintTime = DateTime.Now;

        #region Fields
        private string _header;
        #endregion

        #region Properties
        public string Header
        {
            get { return _header; }
            set { _header = value; }
        }
        #endregion


        public override void OnOpenDocument(PdfWriter writer, Document document)
        {
            try
            {
                PrintTime = DateTime.Now;
                bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                cb = writer.DirectContent;

            }
            catch (DocumentException)
            {
                //handle exception here
            }
            catch (System.IO.IOException)
            {
                //handle exception here
            }
        }

        public override void OnStartPage(iTextSharp.text.pdf.PdfWriter writer, iTextSharp.text.Document document)
        {
            document.SetMargins(15f, 15f, 15f, 15f);
            base.OnStartPage(writer, document);
        }

        public override void OnEndPage(iTextSharp.text.pdf.PdfWriter writer, iTextSharp.text.Document document)
        {
            base.OnEndPage(writer, document);

            //iTextSharp.text.Font baseFontNormal = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.TIMES_ROMAN, 12f, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.BLACK);
            //iTextSharp.text.Font baseFontBig = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.TIMES_ROMAN, 14f, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);
            //iTextSharp.text.Font baseFontBold = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.TIMES_ROMAN, 12f, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);

            BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            BaseFont baseFontBold = BaseFont.CreateFont(boldFontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

            if (writer.PageNumber == 1)
            {
                //Create PdfTable object
                PdfPTable pdfTab = new PdfPTable(3);

                //Row 2
                PdfPCell pdfCell8 = new PdfPCell(new Phrase(fullName, new Font(baseFontBold, 12f, Font.BOLD, BaseColor.BLACK)));
                pdfCell8.PaddingLeft = 70f;
                Phrase phrase = new Phrase();
                phrase.Add(new Chunk(recordHeader, new Font(baseFont, 12f, 1, BaseColor.BLACK)));
                phrase.Add(new Chunk(" " + startDate + " ", new Font(baseFont, 12f, 1, BaseColor.BLACK)));
                phrase.Add(new Chunk(To, new Font(baseFont, 12f, 1, BaseColor.BLACK)));
                phrase.Add(new Chunk(" " + endDate, new Font(baseFont, 12f, 1, BaseColor.BLACK)));

                PdfPCell pdfCell4 = new PdfPCell(phrase);
                pdfCell4.PaddingLeft = 70f;
                //Row 3

                iTextSharp.text.Image myImage = iTextSharp.text.Image.GetInstance(logoPath);
                myImage.ScaleToFit(50f, 50f);

                PdfPCell pdfCell5 = new PdfPCell(new Phrase(RenewalHeader, new Font(baseFontBold, 14f, 1, BaseColor.BLACK)));
                pdfCell5.PaddingTop = 0f;
                pdfCell5.PaddingLeft = 70f;
                pdfCell5.Top = 0f;
                PdfPCell pdfCell6 = new PdfPCell();
                pdfCell6.PaddingTop = 0f;
                pdfCell6.Top = 0f;
                PdfPCell pdfCell7 = new PdfPCell(myImage);
                pdfCell7.Top = 0f;
                pdfCell7.PaddingTop = 4f;
                pdfCell7.PaddingRight = 10f;


                //set the alignment of all three cells and set border to 0
                pdfCell8.HorizontalAlignment = Element.ALIGN_LEFT;
                pdfCell4.HorizontalAlignment = Element.ALIGN_LEFT;
                pdfCell5.HorizontalAlignment = Element.ALIGN_LEFT;
                pdfCell6.HorizontalAlignment = Element.ALIGN_CENTER;
                pdfCell7.HorizontalAlignment = Element.ALIGN_CENTER;


                pdfCell4.VerticalAlignment = Element.ALIGN_TOP;
                pdfCell8.VerticalAlignment = Element.ALIGN_TOP;
                pdfCell5.VerticalAlignment = Element.ALIGN_TOP;
                pdfCell6.VerticalAlignment = Element.ALIGN_TOP;
                pdfCell7.VerticalAlignment = Element.ALIGN_TOP;


                pdfCell4.Colspan = 3;
                pdfCell8.Colspan = 3;

                pdfCell4.Border = 0;
                pdfCell5.Border = 0;
                pdfCell6.Border = 0;
                pdfCell7.Border = 0;
                pdfCell8.Border = 0;

                pdfTab.AddCell(pdfCell5);
                pdfTab.AddCell(pdfCell6);
                pdfTab.AddCell(pdfCell7);
                pdfTab.AddCell(pdfCell8);
                pdfTab.AddCell(pdfCell4);
                pdfTab.TotalWidth = document.PageSize.Width;
                pdfTab.WidthPercentage = 100;

                //call WriteSelectedRows of PdfTable. This writes rows from PdfWriter in PdfTable
                //first param is start row. -1 indicates there is no end row and all the rows to be included to write
                //Third and fourth param is x and y position to start writing
                pdfTab.WriteSelectedRows(0, -1, 0, document.PageSize.Height - 30, writer.DirectContent);
            }
            else
            {
            }
        }

        public override void OnCloseDocument(PdfWriter writer, Document document)
        {
            base.OnCloseDocument(writer, document);
        }
    }
}
