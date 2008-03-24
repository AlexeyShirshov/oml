using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public enum QueryType
    {
        TypeCycleWithoutLoad,
        TypeCycleWithLoad,
        TypeCycleLazyLoad,
        SmallCollectionByIdArray,
        SmallCollection,
        SmallCollectionWithChildrenByIdArray,
        LargeCollectionByIdArray,
        LargeCollection,
        LargeCollectionWithChildrenByIdArray,
        CollectionByPredicateWithoutLoad,
        CollectionByPredicateWithLoad,
        SelectLargeCollection,
        SameObjectInCycleLoad,
        SelectBySamePredicate,
        ObjectsWithLoadWithPropertiesAccess
    }

    public enum Syntax
    {
        Default,
        Linq
    }

    public static class TypeInfo {
        static Dictionary<QueryType, string> types = new Dictionary<QueryType, string>();
        public static Dictionary<QueryType, string> Types
        {
            get { return types; }
        }

        static TypeInfo()
        {
            types.Add(QueryType.TypeCycleWithoutLoad, "1. Загрузка объекта одного типа в цикле (1000 итераций, разные ид) без загрузки состояния");
            types.Add(QueryType.TypeCycleWithLoad, "2. Загрузка объекта одного типа в цикле (1000 итераций, разные ид) с состоянием");
            types.Add(QueryType.TypeCycleLazyLoad, "3. Загрузка объекта одного типа в цикле (1000 итераций, разные ид) без загрузки состояния с последующим обращением к какому-либо свойству (lazy-load)");
            types.Add(QueryType.SmallCollectionByIdArray, "4. Загрузка пачки объектов по массиву идентификаторов (небольшая коллекция)");
            types.Add(QueryType.SmallCollection, "5. Загрузка пачки объектов (небольшая коллекция)");
            types.Add(QueryType.SmallCollectionWithChildrenByIdArray, "6. Загрузка пачки объектов и их дочерних коллекций по массиву идентификаторов (небольшая коллекция)");
            types.Add(QueryType.LargeCollectionByIdArray, "7. Загрузка пачки объектов по массиву идентификаторов (большая коллекция)");
            types.Add(QueryType.LargeCollection, "8. Загрузка пачки объектов (большая коллекция)");
            types.Add(QueryType.LargeCollectionWithChildrenByIdArray, "9. Загрузка пачки объектов и их дочерних коллекций по массиву идентификаторов (большая коллекция)");
            types.Add(QueryType.CollectionByPredicateWithoutLoad, "10. Выборка по определеному предикату коллекций (1000 итераций, разные условия выборки) без загрузки");
            types.Add(QueryType.CollectionByPredicateWithLoad, "11. Выборка по определеному предикату коллекций (1000 итераций, разные условия выборки) с загрузкой");
            types.Add(QueryType.SelectLargeCollection, "12. Выборка большой коллекции (10 итераций)");
            types.Add(QueryType.SameObjectInCycleLoad, "13. Загрузка одного и того же объекта в цикле (10 интераций)");
            types.Add(QueryType.SelectBySamePredicate, "14. Выборка по одному и тому же предикату (10 итераций)");
            types.Add(QueryType.ObjectsWithLoadWithPropertiesAccess, "15. Выборка объектов с загрузкой и произвольный доступ к ним в цикле (для каждого объекта)");
        }
    }

    public class Constants
    {
        public const int Small = 10;
        public const int Medium = 100;
        public const int Large = 2000;

        public const int SmallIteration = 10;
        public const int LargeIteration = 1000;
    }
}
