using System;
using System.Collections.Generic;

namespace My_Api
{
    public partial class MyBlog
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string Tags { get; set; }
        public int Author { get; set; }
        public string CoverImage { get; set; }
        public string ThumbNail { get; set; }
        public string PermaLink { get; set; }
        public int? TimeRequired { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime DateCreated { get; set; }
        public sbyte Draft { get; set; }
        public string Slug { get; set; }
    }
}
