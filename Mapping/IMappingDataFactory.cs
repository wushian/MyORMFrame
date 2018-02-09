﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping
{
    public interface IMappingDataFactory
    {
        Dictionary<Type, IMappingData> GetMappingData(Type type);
    }
}