using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReadToExcell.Entities;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;

namespace ReadToExcell
{
    class Program
    {
        static List<Files> FileListControl;
        static AykutGurselDBEntities db = new AykutGurselDBEntities();
        static void Main(string[] args)
        {
            DocumentControl();
            WriteDatabase();

            Console.WriteLine("OK!");
            Console.ReadKey();
        }

        public static void DocumentControl()
        {
            FileListControl = new List<Files>();

            DirectoryInfo directory = new DirectoryInfo("Files"); //Dosyaların bulunduğu klasör bin/Debug/Files/
            FileInfo[] files = directory.GetFiles("*.xlsx");    //Kontrol edilecek dosyaların uzantıları

            foreach (FileInfo file in files.Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden)))
            {
                var dbFile = db.Files.Where(x => x.FilesName == file.FullName).FirstOrDefault();

                if (dbFile == null)
                {
                    FileListControl.Add(new Files
                    {
                        FilesName = file.FullName,
                        IsComplete = false
                    });
                }
                else if (dbFile.IsComplete == true || dbFile.IsComplete == false) continue;
            }

            db.Files.AddRange(FileListControl);
            db.SaveChanges();
        }

        public static void WriteDatabase()
        {
            foreach (var file in db.Files.Where(x => x.IsComplete == false).ToList())
            {
                var currentFileDatas = GetDataFromExcel(file.FilesName);

                foreach (var item in currentFileDatas)
                {
                    FileDatas fd = new FileDatas
                    {
                        UniqueId = int.Parse(item[0]),
                        FirstName = item[1],
                        LastName = item[2],
                        Age = int.Parse(item[3]),
                        University = item[4],
                        FilesId = file.Id
                    };

                    db.FileDatas.Add(fd);
                }

                var fileUpdate = db.Files.Where(x => x.Id == file.Id).First();
                fileUpdate.IsComplete = true;
                db.SaveChanges();
            }
        }

        public static List<List<string>> GetDataFromExcel(string filepath)
        {


            List<List<string>> lst = new List<List<string>>();
            try
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filepath, false))
                {

                    WorkbookPart wbPart = doc.WorkbookPart;

                    int worksheetcount = doc.WorkbookPart.Workbook.Sheets.Count();

                    Sheet mysheet = (Sheet)doc.WorkbookPart.Workbook.Sheets.ChildElements.GetItem(0);

                    Worksheet Worksheet = ((WorksheetPart)wbPart.GetPartById(mysheet.Id)).Worksheet;

                    int wkschildno = 4;

                    SheetData Rows = (SheetData)Worksheet.ChildElements.GetItem(wkschildno);
                    Row currentrow2 = (Row)Rows.ChildElements.GetItem(0);

                    for (int i = 1; i < Rows.ChildElements.Count; i++)
                    {
                        Row currentrow = (Row)Rows.ChildElements.GetItem(i);
                        List<string> lstrow = new List<string>();

                        int k = 0;
                        for (int j = 0; j < currentrow2.ChildElements.Count; j++)
                        {
                            Cell currentcell2 = (Cell)currentrow2.ChildElements.GetItem(j);
                            Cell currentcell = (Cell)currentrow.ChildElements.GetItem(k);
                            if (Regex.Replace(currentcell.CellReference.ToString(), "[0-9]", "") == Regex.Replace(currentcell2.CellReference.ToString(), "[0-9]", ""))
                            {
                                lstrow.Add(GetValue(doc, currentcell));
                                if (k < currentrow.ChildElements.Count - 1)
                                {
                                    k++;
                                }
                            }
                            else
                            {
                                lstrow.Add(String.Empty);
                            }
                        }
                        lst.Add(lstrow);
                    }

                    return lst;


                }
            }
            catch (Exception Ex)
            {

                string excep = Ex.Message;
                //SendEmail("systeminfo@telekurye.com.tr", "Burhan.INCE@telekurye.com.tr", "", "GetDataFromExcel", "HATA:" + excep, null);
            }
            return lst;
        }

        private static string GetValue(SpreadsheetDocument doc, Cell cell)
        {
            try
            {
                if (cell.CellValue != null)
                {
                    string value = cell.CellValue.InnerText;
                    if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                    {
                        return doc.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements.GetItem(int.Parse(value)).InnerText;
                    }
                    return value;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }



    }


}
