using iTextSharp.text;
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
                bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                cb = writer.DirectContent;
            }
            catch (DocumentException de)
            {
                //handle exception here
            }
            catch (System.IO.IOException ioe)
            {
                //handle exception here
            }
        }

        public override void OnEndPage(iTextSharp.text.pdf.PdfWriter writer, iTextSharp.text.Document document)
        {
            base.OnEndPage(writer, document);

            iTextSharp.text.Font baseFontNormal = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 12f, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.BLACK);
            iTextSharp.text.Font baseFontFooter = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10f, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.BLACK);
            iTextSharp.text.Font baseFontBig = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 14f, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);
            
            //Create PdfTable object
            PdfPTable pdfTab = new PdfPTable(3);

            //Row 2
            PdfPCell pdfCell8 = new PdfPCell(new Phrase(fullName, baseFontBig));

            PdfPCell pdfCell4 = new PdfPCell(new Phrase("A record of your giving from " + startDate + " to " + endDate, baseFontNormal));
            //Row 3

            iTextSharp.text.Image myImage = iTextSharp.text.Image.GetInstance(logoPath);
            myImage.ScaleToFit(50f, 50f);
            PdfPCell pdfCell5 = new PdfPCell(new Phrase("The Renewal Center", baseFontNormal));
            PdfPCell pdfCell6 = new PdfPCell();
            PdfPCell pdfCell7 = new PdfPCell(myImage);


            //set the alignment of all three cells and set border to 0
            pdfCell8.HorizontalAlignment = Element.ALIGN_CENTER;
            pdfCell4.HorizontalAlignment = Element.ALIGN_CENTER;
            pdfCell5.HorizontalAlignment = Element.ALIGN_CENTER;
            pdfCell6.HorizontalAlignment = Element.ALIGN_CENTER;
            pdfCell7.HorizontalAlignment = Element.ALIGN_CENTER;


            pdfCell4.VerticalAlignment = Element.ALIGN_TOP;
            pdfCell8.VerticalAlignment = Element.ALIGN_TOP;
            pdfCell5.VerticalAlignment = Element.ALIGN_MIDDLE;
            pdfCell6.VerticalAlignment = Element.ALIGN_MIDDLE;
            pdfCell7.VerticalAlignment = Element.ALIGN_MIDDLE;


            pdfCell4.Colspan = 3;
            pdfCell8.Colspan = 3;

            pdfCell4.Border = 0;
            pdfCell5.Border = 0;
            pdfCell6.Border = 0;
            pdfCell7.Border = 0;
            pdfCell8.Border = 0;
            
            pdfTab.AddCell(pdfCell8);
            pdfTab.AddCell(pdfCell4);
            pdfTab.AddCell(pdfCell5);
            pdfTab.AddCell(pdfCell6);
            pdfTab.AddCell(pdfCell7);
            pdfTab.TotalWidth = document.PageSize.Width;
            pdfTab.WidthPercentage = 70;

            //call WriteSelectedRows of PdfTable. This writes rows from PdfWriter in PdfTable
            //first param is start row. -1 indicates there is no end row and all the rows to be included to write
            //Third and fourth param is x and y position to start writing
            pdfTab.WriteSelectedRows(0, -1, 10, document.PageSize.Height - 30, writer.DirectContent);
            
            PdfPTable pdfTabFooter = new PdfPTable(1);
            pdfTabFooter.TotalWidth = document.PageSize.Width - 40f;
            pdfTabFooter.WidthPercentage = 70;

            if (isAdd == true)
            {
                PdfPCell pdfCellFooter = new PdfPCell(new Phrase("The Renewal Center is recognized as exempt under section 501(c)(3) of the Internal Revenue Code in the United States.Donors may deduct contributions as provided in section 170 of the Code.", baseFontFooter));
                pdfCellFooter.VerticalAlignment = Element.ALIGN_BOTTOM;
                pdfCellFooter.HorizontalAlignment = Element.ALIGN_CENTER;
                pdfCellFooter.Border = 0;
                pdfTabFooter.AddCell(pdfCellFooter);
            }

            iTextSharp.text.Image footerImage = iTextSharp.text.Image.GetInstance(sealImagePath);
            footerImage.ScaleToFit(50f, 50f);
            PdfPCell pdfCellFooterImage = new PdfPCell(footerImage);
            pdfCellFooterImage.VerticalAlignment = Element.ALIGN_BOTTOM;
            pdfCellFooterImage.HorizontalAlignment = Element.ALIGN_CENTER;
            pdfCellFooterImage.Border = 0;
            pdfCellFooterImage.PaddingTop = 10f;
            pdfTabFooter.AddCell(pdfCellFooterImage);

            //Move the pointer and draw line to separate header section from rest of page
            cb.MoveTo(40, document.PageSize.Height - 130);
            cb.LineTo(document.PageSize.Width - 40, document.PageSize.Height - 130);
            cb.Stroke();

            if (isAdd == true)
            {
                pdfTabFooter.WriteSelectedRows(0, -1, 20, document.PageSize.GetBottom(100), writer.DirectContent);
            }
            else
            {
                pdfTabFooter.WriteSelectedRows(0, -1, 20, document.PageSize.GetBottom(80), writer.DirectContent);
            }
            
            if (isAdd == true)
            {
                //Move the pointer and draw line to separate footer section from rest of page
                cb.MoveTo(40, document.PageSize.GetBottom(110));
                cb.LineTo(document.PageSize.Width - 40, document.PageSize.GetBottom(110));
                cb.Stroke();
            }
            else
            {
                cb.MoveTo(40, document.PageSize.GetBottom(90));
                cb.LineTo(document.PageSize.Width - 40, document.PageSize.GetBottom(90));
                cb.Stroke();
            }
        }

        public override void OnCloseDocument(PdfWriter writer, Document document)
        {
            base.OnCloseDocument(writer, document);
        }
    }
}
