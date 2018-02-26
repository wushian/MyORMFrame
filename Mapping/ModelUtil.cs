﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyORMFrame.Attributes;

namespace MyORMFrame.Mapping
{
    public class ModelUtil
    {
        public string PrimaryKeyPropertyName { get; set; }

        public Type ModelType { get; set; }

        /// <summary>
        /// 构造函数：初始化属性, 要保证model各属性标注合法，并确认唯一主键
        /// </summary>
        /// <param name="type"></param>
        public ModelUtil(Type type)
        {
            this.ModelType = type;
            //初始化
            VertifyDbAttributes();    
        }
        public List<RelationModel> GetRelations()
        {
            List<RelationModel> relations = new List<RelationModel>();

            RelationModel mainRelation = new RelationModel(ModelType.Name);

            relations.Add(mainRelation);

            var propertys = ModelType.GetProperties();
            foreach (var p in propertys)
            {
                RelationModelColumn r_column = null;
                List<RelationModel> new_relations = null;

                ModelUtil otherModel_util = null;

                string dbType = null;
                string typeSize = null;
                string constraints = string.Empty;

                foreach (var attr in GetPropertyDbAtttibutes(p.Name))
                {
                    if (attr is ConstraintAttribute)
                    {
                        constraints += ((ConstraintAttribute)attr).GetConstraintStr();
                    }
                }

                var propertyMappingInfo = GetPropertyMappingInfo(p.Name);
                switch (propertyMappingInfo.Property_TypeRole)
                {
                    case PropertyMappingInfo.PropertyTypeRole.Value:

                        dbType = GetPropertyDbAttribute<TypeAttribute>(p.Name).DbTypeName;
                        typeSize = GetPropertyDbAttribute<TypeAttribute>(p.Name).Size;

                        r_column = new RelationModelColumn(p.Name, dbType, typeSize, constraints);               

                        break;

                    case PropertyMappingInfo.PropertyTypeRole.Model:

                        otherModel_util = new ModelUtil(propertyMappingInfo.ReferenceModelType);

                        dbType = otherModel_util.GetPropertyDbAttribute<TypeAttribute>(otherModel_util.PrimaryKeyPropertyName).DbTypeName;
                        typeSize = otherModel_util.GetPropertyDbAttribute<TypeAttribute>(otherModel_util.PrimaryKeyPropertyName).Size;

                        r_column = new RelationModelColumn(p.Name, dbType, typeSize, "NOT NULL");    //  设置外键

                        new_relations = otherModel_util.GetRelations();

                        break;

                    case PropertyMappingInfo.PropertyTypeRole.ModelList_To_Obj:

                        otherModel_util = new ModelUtil(propertyMappingInfo.ReferenceModelType);

                        new_relations = otherModel_util.GetRelations();

                        dbType = GetPropertyDbAttribute<TypeAttribute>(PrimaryKeyPropertyName).DbTypeName;
                        typeSize = GetPropertyDbAttribute<TypeAttribute>(PrimaryKeyPropertyName).Size;

                        //被参照关系的主关系添加一列参照属性
                        new_relations[0].Columns.Add(new RelationModelColumn(PrimaryKeyPropertyName, dbType, typeSize, "NOT NULL"));

                        break;

                    case PropertyMappingInfo.PropertyTypeRole.ModelList_To_List:

                        otherModel_util = new ModelUtil(propertyMappingInfo.ReferenceModelType);
                        //新建第三方参照relation
                        new_relations = new List<RelationModel>();
                        var _newRelation = new RelationModel(string.Format("{0}_{1}", ModelType.Name, otherModel_util.ModelType.Name));

                        //添加第一参照列
                        dbType = GetPropertyDbAttribute<TypeAttribute>(PrimaryKeyPropertyName).DbTypeName;
                        typeSize = GetPropertyDbAttribute<TypeAttribute>(PrimaryKeyPropertyName).Size;

                        _newRelation.Columns.Add(new RelationModelColumn(PrimaryKeyPropertyName, dbType, typeSize, "NOT NULL"));

                        //添加第二参照列
                        dbType = GetPropertyDbAttribute<TypeAttribute>(otherModel_util.PrimaryKeyPropertyName).DbTypeName;
                        typeSize = GetPropertyDbAttribute<TypeAttribute>(otherModel_util.PrimaryKeyPropertyName).Size;

                        _newRelation.Columns.Add(new RelationModelColumn(otherModel_util.PrimaryKeyPropertyName, dbType, typeSize, "NOT NULL"));

                        //添加第三参照关系
                        new_relations.Add(_newRelation);
                           
                        break;
                }

                mainRelation.Columns.Add(r_column);

                if (new_relations != null)
                {
                    relations.AddRange(new_relations);
                }
                              
                new_relations = null;   //  清空新建关系
            }

            return relations;
        }

        public object GetPrimaryKeyValue(object obj)
        {
            return GetPropertyValue(obj, PrimaryKeyPropertyName);
        }

        public object GetPropertyValue(object obj, string propertyName)
        {
            if (obj.GetType().Equals(ModelType)) //检查类型
            {
                return ModelType.GetProperty(propertyName).GetValue(obj);
            }
            else
            {
                return null;
            }            
        }

        public void SetPropertyValue<TValue>(object obj, string propertyName, TValue value)
        {
            if (obj.GetType().Equals(ModelType)) //检查类型
            {
                Type valueType = typeof(TValue);
                switch (valueType.Name)
                {
                    case "System.Int32":
                        ModelType.GetProperty(propertyName).SetValue(obj, int.Parse(value.ToString()));
                        break;
                    case "System.String":
                        break;
                    case "System.Bool":
                        break;
                    default:
                        ModelType.GetProperty(propertyName).SetValue(obj, value);
                        break;
                }
            }
        }

        public PropertyMappingInfo GetPropertyMappingInfo(string propertyName)
        {
            PropertyMappingInfo info = new PropertyMappingInfo();

            info.DbColumnName = propertyName;

            var property = ModelType.GetProperty(propertyName);

            Type propertyType = property.PropertyType;

            string propertyDbTypeName = GetPropertyDbAttribute<TypeAttribute>(propertyName).DbTypeName;

            if (propertyDbTypeName == DbTypeMapping.UserType.TypeName)
            {
                //若映射类型名为null的话,则说明该列属性存在引用关系
                if (propertyType.GetInterface("IEnumerable`1") != null)
                {
                    //如果是集合,则验证是多对一，还是多对多关系。
                    var argTypes = propertyType.GetGenericArguments();//获取集合中的元素的数据类型
                    if (argTypes.Length == 1)
                    {
                        //若是合法集合，验证是多对一，还是多对多关系。
                        var referenceModelType = argTypes[0];

                        info.ReferenceModelType = referenceModelType;
                        //验证目标model是否包含关于自身的泛型集合，以确定是多对一还是多对多
                        bool isContainListOfSelf = false;
                        foreach (var p in referenceModelType.GetProperties())
                        {
                            if (p.PropertyType.GetInterface("IEnumerable`1") != null)
                            {
                                var other_argTypes = p.PropertyType.GetGenericArguments();
                                if (other_argTypes.Length == 1 && other_argTypes[0].Equals(ModelType))
                                {
                                    isContainListOfSelf = true;
                                    break;
                                }
                            }
                        }

                        if (!isContainListOfSelf)
                        {
                            //多对一关系，验证自身是否包含主键属性
                            if (PrimaryKeyPropertyName != null)
                            {                            
                                info.Property_TypeRole = PropertyMappingInfo.PropertyTypeRole.ModelList_To_Obj;
                            }
                            else
                            {
                                //否则抛出异常
                            }
                        }
                        else
                        {
                            //多对多关系，验证双方是否包含主键属性
                            string other_primaryKeyName = new ModelUtil(info.ReferenceModelType).PrimaryKeyPropertyName;
                            if (PrimaryKeyPropertyName != null && other_primaryKeyName != null)
                            {
                                info.Property_TypeRole = PropertyMappingInfo.PropertyTypeRole.ModelList_To_List;
                            }
                            else
                            {
                                //否则抛出异常
                            }
                            
                        }
                        
                    }
                    else
                    {
                        //不接受多类型泛型集合，抛出异常
                    }
                }
                else
                {
                    //否则是单引用关系
                    //若是单引用关系,则目标关系必须包含主键(外键)
                    string other_primaryKeyName = new ModelUtil(propertyType).PrimaryKeyPropertyName;
                    if (other_primaryKeyName != null)   //  验证外键
                    {
                        info.Property_TypeRole = PropertyMappingInfo.PropertyTypeRole.Model;
                        info.ReferenceModelType = propertyType;
                    }
                    else
                    {
                        //否则抛出异常

                    }
                    
                }
            }
            else
            {
                info.Property_TypeRole = PropertyMappingInfo.PropertyTypeRole.Value;
            }
            return info;
        }

        public void VertifyDbAttributes()
        {

        }

        public TAttributeType GetPropertyDbAttribute<TAttributeType>(string propertyName) where TAttributeType : DbAttribute
        {
            var attrs = GetPropertyDbAtttibutes(propertyName);

            TAttributeType a = null;
            foreach(var attr in attrs)
            {
                if (attr is TAttributeType)
                {
                    a = (TAttributeType)attr;
                    break;
                }
            }
            return a;
        }

        public List<DbAttribute> GetPropertyDbAtttibutes(string propertyName)
        {
            List<DbAttribute> dbAttributes = new List<DbAttribute>();

            var property = ModelType.GetProperty(propertyName);

            var attrs = System.Attribute.GetCustomAttributes(property);

            foreach (var attr in attrs)
            {
                if (attr is DbAttribute)
                {
                    dbAttributes.Add((DbAttribute)attr);
                }
            }

            return dbAttributes;

        }

    }
}
