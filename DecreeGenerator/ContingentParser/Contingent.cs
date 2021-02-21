using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ContingentParser
{
    /// <summary>
    /// Реализует сущность "Контингент", котороя представляет собой множество всех студентов факультета
    /// </summary>
    public class Contingent : IEnumerable<Student>
    {
        private readonly HashSet<Student> students;

        /// <summary>
        /// Инициализирует сущность "Контингент" множеством студентов
        /// </summary>
        /// <param name="fileName">Имя файла с расширением .xslx, в котором содержится информация о студентах</param>
        public Contingent(string fileName)
        {
            using var spreadsheetDocument = SpreadsheetDocument.Open(fileName, false);
            var sharedStringTable = spreadsheetDocument.WorkbookPart.SharedStringTablePart.SharedStringTable;
            var workSheet = spreadsheetDocument.WorkbookPart.WorksheetParts.First().Worksheet;

            students = Parse(sharedStringTable, workSheet);
        }

        /// <summary>
        /// Извлекает основную информацию о студентах из файла с контингентом
        /// </summary>
        /// <param name="sharedStringTable">Таблица разделяемых строк</param>
        /// <param name="worksheet">Рабочий лист</param>
        /// <returns>Набор сущностей, которые описывают каждого студента в контингенте</returns>
        public HashSet<Student> Parse(SharedStringTable sharedStringTable, Worksheet worksheet)
        {
            var students = new HashSet<Student>();
            var sharedStringItems = new Dictionary<string, string>();
            var sharedItems = sharedStringTable.Elements<SharedStringItem>().ToList();
            for (var i = 0; i < sharedItems.Count; ++i)
            {
                sharedStringItems.Add(i.ToString(), sharedItems[i].InnerText);
            }

            var sheetData = worksheet.Elements<SheetData>().First();
            var rows = sheetData.Elements<Row>().ToList();
            for (var i = 1; i < rows.Count(); ++i)
            {
                var j = 0;
                var cells = rows[i].Elements<Cell>().ToList();
                var surname = sharedStringItems[cells[j].CellValue.InnerText];
                var name = sharedStringItems[cells[j + 1].CellValue.InnerText];
                var patronymic = sharedStringItems[cells[j + 2].CellValue.InnerText];
                var group = sharedStringItems[cells[j + 3].CellValue.InnerText].Split(' ')[0];
                var programmeCode = ParseProgrammeCode(sharedStringItems[cells[j + 4].CellValue.InnerText]);
                var specialization = sharedStringItems[cells[j + 5].CellValue.InnerText];
                var educationState = sharedStringItems[cells[j + 6].CellValue.InnerText];
                var levelOfStudy = sharedStringItems[cells[j + 7].CellValue.InnerText];
                var curriculumCode = ParseCurriculumCode(sharedStringItems[cells[j + 8].CellValue.InnerText]);
                var educationalPeriod = sharedStringItems[cells[j + 10].CellValue.InnerText];
                var course = sharedStringItems[cells[j + 11].CellValue.InnerText];
                var status = sharedStringItems[cells[j + 13].CellValue.InnerText];
                var averageScore = cells[j + 15].CellValue.InnerText;
                var yearOfAdmission = sharedStringItems[cells[j + 18].CellValue.InnerText];

                var student = new Student(surname, name, patronymic, levelOfStudy,
                    programmeCode, curriculumCode, status, specialization, course, group,
                    yearOfAdmission, averageScore, educationState);

                students.Add(student);
            }

            return students;
        }

        /// <summary>
        /// Извлекает код программы, на которой обучается студент, из передаваемой строки
        /// </summary>
        /// <param name="line">Строка, содержащая код программы с разделителем '.'</param>
        /// <returns>Код программы с разделителем '.'</returns>
        private string ParseProgrammeCode(string line)
        {
            var match = Regex.Match(line, @"\d{2}\.\d{2}\.\d{2}");

            return match.Value;
        }

        /// <summary>
        /// Из передаваемой строки извлекает код учебного плана, по которому обучается студент
        /// </summary>
        /// <param name="line">Строка, содержащая код учебного плана с разделителем'\'</param>
        /// <returns>Код учебного плана с разделителем '\'</returns>
        private string ParseCurriculumCode(string line)
        {
            var match = Regex.Match(line, @"\d{2}\\\d{4}\\\d{1}");

            return match.Value;
        }

        /// <summary>
        /// Возвращает перечислитель, который перечисляет студентов в
        /// перечислимом экземляре класса <name>Contingent</name>
        /// </summary>
        /// <returns>Перечислитель</returns>
        public IEnumerator<Student> GetEnumerator()
            => students.GetEnumerator();

        /// <summary>
        /// Возвращает перечислитель, который перечисляет студентов в
        /// перечислимом экземляре класса <name>Contingent</name>
        /// </summary>
        /// <returns>Перечислитель</returns>
        IEnumerator IEnumerable.GetEnumerator()
            => students.GetEnumerator();
    }
}
