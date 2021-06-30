using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Properties;
using kOS.Safe.Serialization;
using kOS.Safe.Function;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("List")]
    public class ListValue<T> : CollectionValue<T, List<T>>, IList<T>, IIndexable
        where T : Structure
    {
        public ListValue()
            : this(new List<T>())
        {
        }

        public ListValue(IEnumerable<T> listValue) : base("LIST", new List<T>(listValue))
        {
            RegisterInitializer(ListInitializeSuffixes);
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static ListValue<T> CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new ListValue<T>();
            newObj.LoadDump(d);
            return newObj;
        }

        public int Count => Collection.Count;
        public void CopyTo(T[] array, int arrayIndex) =>
            Collection.CopyTo(array, arrayIndex);

        public void Add(T item)
        {
            CheckReadOnly();
            Collection.Add(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            CheckReadOnly();
            Collection.AddRange(items);
        }

        public bool Remove(T item)
        {
            CheckReadOnly();
            return Collection.Remove(item);
        }

        public void RemoveAt(int index)
        {
            CheckReadOnly();
            Collection.RemoveAt(GetAbsoluteIndex(index));
        }

        public T this[int index]
        {
            get { return Collection[index]; }
            set
            {
                CheckReadOnly();
                Collection[index] = value;
            }
        }

        public override void LoadDump(Dump dump)
        {
            Collection.Clear();

            List<object> values = (List<object>)dump[kOS.Safe.Dump.Items];

            foreach (object item in values)
            {
                Collection.Add((T)FromPrimitive(item));
            }
        }

        private void ListInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<ListValue<T>>        (() => new ListValue<T>(this)));
            AddSuffix("ADD",      new OneArgsSuffix<T>                  (toAdd => Add(toAdd), Resources.ListAddDescription));
            AddSuffix("ADDRANGE", new OneArgsSuffix<ListValue<T>>       (toAdd => AddRange(toAdd)));
            AddSuffix("INSERT",   new TwoArgsSuffix<ScalarValue, T>     ((index, toAdd) => Insert(index, toAdd)));
            AddSuffix("REMOVE",   new OneArgsSuffix<ScalarValue>        (toRemove => RemoveAt(toRemove)));
            AddSuffix("SUBLIST",  new TwoArgsSuffix<ListValue, ScalarValue, ScalarValue>(SubListMethod));
            AddSuffix("JOIN",     new OneArgsSuffix<StringValue, StringValue>(Join));

            AddSuffix(new[] { "INDEXOF", "FIND" }, new OneArgsSuffix<ScalarValue, T>(one => IndexOf(one)));
            AddSuffix(new[] { "LASTINDEXOF", "FINDLAST" }, new OneArgsSuffix<ScalarValue, T>(s => Collection.LastIndexOf(s)));
        }

        // This test case was added to ensure there was an example method with more than 1 argument.
        private ListValue SubListMethod(ScalarValue start, ScalarValue runLength)
        {
            var subList = new ListValue();
            for (int i = start; i < Collection.Count && i < start + runLength; ++i)
            {
                subList.Add(Collection[i]);
            }
            return subList;
        }

        public static ListValue<T> CreateList<TU>(IEnumerable<TU> list) =>
            new ListValue<T>(list.Cast<T>());

        public Structure GetIndex(int index)
        {
            return Collection[GetAbsoluteIndex(index)];
        }

        public Structure GetIndex(Structure index)
        {
            return GetIndex(GetIntIndex(index));
        }

        public void SetIndex(Structure index, Structure value)
        {
            SetIndex(GetIntIndex(index), value);
        }

        public void SetIndex(int index, Structure value)
        {
            CheckReadOnly();
            Collection[GetAbsoluteIndex(index)] = (T)value;
        }

        private StringValue Join(StringValue separator) =>
            string.Join(separator, Collection.Select(i => i.ToString()).ToArray());

        public int IndexOf(T item) =>
            Collection.IndexOf(item);
        public void Insert(int index, T item)
        {
            CheckReadOnly();
            Collection.Insert(GetAbsoluteIndex(index), item);
        }

        private int GetAbsoluteIndex(int index)
        {
            // support for negative indexes, a value of -1 returns the last element, of -2 the second-last element, and so on.
            return index + (index < 0 ? Collection.Count : 0);
        }

        private int GetIntIndex(Structure index)
        {
            if (index is ScalarValue)
            {
                return Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
            }
            // Throw cast exception with ScalarIntValue, instead of just any ScalarValue
            throw new KOSCastException(index.GetType(), typeof(ScalarIntValue));
        }

        public static ListValue<T> operator +(ListValue<T> a, ListValue<T> b)
        {
            return new ListValue<T>(a.Concat(b));
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("List", KOSToCSharp = false)] // one-way because the generic templated ListValue<T> is the canonical one.  
    public class ListValue : ListValue<Structure>
    {
        [Function("list")]
        public class FunctionList : SafeFunctionBase
        {
            public override void Execute(SafeSharedObjects shared)
            {
                Structure[] argArray = new Structure[CountRemainingArgs(shared)];
                for (int i = argArray.Length - 1; i >= 0; --i)
                    argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
                AssertArgBottomAndConsume(shared);
                var listValue = new ListValue(argArray.ToList());
                ReturnValue = listValue;
            }
        }

        public ListValue()
        {
            RegisterInitializer(InitializeSuffixes);
        }

        public ListValue(IEnumerable<Structure> toCopy) : base(toCopy)
        {
            RegisterInitializer(InitializeSuffixes);
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static new ListValue CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new ListValue();
            newObj.LoadDump(d);
            return newObj;
        }

        private void InitializeSuffixes() =>
            AddSuffix("COPY", new NoArgsSuffix<ListValue>(() => new ListValue(this)));

        public new static ListValue CreateList<T>(IEnumerable<T> toCopy) =>
            new ListValue(toCopy.Select(x => FromPrimitiveWithAssert(x)));
    }
}



