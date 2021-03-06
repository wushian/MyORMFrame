﻿using System;
using System.Collections.Generic;

namespace MyORMFrame.Mapping
{
    public interface IMappingDataSet
    {
        void InputType(Type type);

        void InputTypes(List<Type> types);

        RelationModel GetRelation(string relationName);

        IMapper GetMapper(Type type);

        List<RelationModel> GetAllRelations();
    }
}