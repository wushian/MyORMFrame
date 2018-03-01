﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyORMFrame.Mapping
{
    public class MappingDataSet : IMappingDataSet
    {
        Dictionary<string, RelationModel> relationModels;   //  用dictionary是方便查找

        Dictionary<Type, IMapper> mappers;

        Func<Type, IMapper> mapperCreator;

        public MappingDataSet(Func<Type, IMapper> mapperCreator)
        {
            this.relationModels = new Dictionary<string, RelationModel>();

            this.mappers = new Dictionary<Type, IMapper>();

            this.mapperCreator = mapperCreator;
        }

        public void InputType(Type type)
        {
            IMapper mapper = mapperCreator(type);       //创建映射器

            mappers.Add(type, mapper);    
              
            var typeRelations = mapper.GetRelations();  //获取该type涉及的所有关系模型

            //添加汇总关系模型集合
            foreach (var relation in typeRelations)
            {
                if (relationModels.Values.Where(a => a.TbName == relation.TbName).ToList().Count == 0)
                {
                    //如果关系模型字典中没有该关系，则直接添加
                    relationModels.Add(relation.TbName, relation);
                }
                else
                {
                    //若字典中已经存在该关系，则汇总该关系中的所有列
                    RelationModel oldRelation = relationModels[relation.TbName];

                    foreach (var column in relation.Columns)
                    {
                        if (oldRelation.Columns.Where( a=> a.ColumnName == column.ColumnName) != null)
                        {
                            //若不存在该列，就添加汇总
                            oldRelation.Columns.Add(column);
                        }
                    }
                }
            }                    
        }
        public void InputTypes(List<Type> types)
        {
            foreach (var type in types)
            {
                InputType(type);
            }
        }

        public List<RelationModel> GetAllRelations()
        {
            return relationModels.Values.ToList();
        }

        public RelationModel GetRelation(string relationName)
        {
            return relationModels[relationName];
        }

        public IMapper GetMapper(Type type)
        {
            return mappers[type];
        }
    }
}
