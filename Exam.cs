using System;
using System.Collections.Generic;

namespace PLG_Exam
{
    public class Exam
    {
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public DateTime? Datum { get; set; }
        public List<ExamTab> Tabs { get; set; } = new();
    }

    public class ExamTab
    {
        public string Aufgabennummer { get; set; } = string.Empty;
        public string Überschrift { get; set; } = string.Empty;
        public string Inhalt { get; set; } = string.Empty;
    }
}
