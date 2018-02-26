﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyORMFrame.Mapping;
using MyORMFrame.Attributes;

namespace MyORMFrame.Test
{
    
    public class Student
    {
        [PrimaryKey]
        public int Id { get; set; }
        public Class Class { get;set;}
        public List<Course> Courses { get; set; }
    }

    public class @Class
    {
        [PrimaryKey]
        public int id { get; set; }
    }

    public class Course
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Student> Students { get; set; }
    }
}
