﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#if TEST_BCL
using System.Collections.Generic;
#else
using Kaos.Collections;
#endif

namespace Kaos.Test.Collections
{
    public partial class TestBtree
    {
        #region Test Keys constructor

        [TestMethod]
        [ExpectedException (typeof (ArgumentNullException))]
        public void CrashRdk_Ctor_ArgumentNull()
        {
            Setup();
#if TEST_BCL
            var keys = new SortedDictionary<int,int>.KeyCollection (null);
#else
            var keys = new RankedDictionary<int,int>.KeyCollection (null);
#endif
        }

        [TestMethod]
        public void UnitRdk_Ctor()
        {
            Setup();
            tree1.Add (1, -1);
#if TEST_BCL
            var keys = new SortedDictionary<int,int>.KeyCollection (tree1);
#else
            var keys = new RankedDictionary<int,int>.KeyCollection (tree1);
#endif
            Assert.AreEqual (1, keys.Count);
        }

        #endregion

        #region Test Keys properties

        [TestMethod]
        public void UnitRdk_Count()
        {
            Setup();
            foreach (int key in iVals1)
                tree1.Add (key, key + 1000);

            Assert.AreEqual (iVals1.Length, tree1.Keys.Count);
        }


        [TestMethod]
        public void UnitRdk_gICollectionIsReadonly()
        {
            Setup();
            var gc = (System.Collections.Generic.ICollection<int>) tree1.Keys;
            Assert.IsTrue (gc.IsReadOnly);
        }


        [TestMethod]
        public void UnitRdk_oICollectionSyncRoot()
        {
            Setup();
            var oc = (System.Collections.ICollection) tree1.Keys;
            Assert.IsFalse (oc.SyncRoot.GetType().IsValueType);
        }

        #endregion

        #region Test Keys methods

        [TestMethod]
        [ExpectedException (typeof (NotSupportedException))]
        public void CrashRdk_gICollectionAdd_NotSupported()
        {
            Setup();
            genKeys2.Add ("omega");
        }


        [TestMethod]
        [ExpectedException (typeof (NotSupportedException))]
        public void CrashRdk_gICollectionClear_NotSupported()
        {
            Setup();
            genKeys2.Clear();
        }


        [TestMethod]
        public void UnitRdk_gICollectionContains()
        {
            Setup();
            tree2.Add ("alpha", 10);
            tree2.Add ("beta", 20);

            Assert.IsTrue (genKeys2.Contains ("beta"));
            Assert.IsFalse (genKeys2.Contains ("zed"));
        }


        [TestMethod]
        [ExpectedException (typeof (ArgumentNullException))]
        public void CrashRdk_CopyTo_ArgumentNull()
        {
            Setup();
            var target = new int[10];
            tree1.Keys.CopyTo (null, -1);
        }

        [TestMethod]
        [ExpectedException (typeof (ArgumentOutOfRangeException))]
        public void CrashRdk_CopyTo_ArgumentOutOfRange()
        {
            Setup();
            var target = new int[iVals1.Length];
            tree1.Keys.CopyTo (target, -1);
        }

        [TestMethod]
        [ExpectedException (typeof (ArgumentException))]
        public void CrashRdk_CopyTo_Argument()
        {
            Setup();
            for (int key = 1; key < 10; ++key)
                tree1.Add (key, key + 1000);

            var target = new int[4];
            tree1.Keys.CopyTo (target, 2);
        }

        [TestMethod]
        public void UnitRdk_CopyTo()
        {
            int n = 10;
            int offset = 5;
            Setup();
            for (int k = 0; k < n; ++k)
                tree1.Add (k, k + 1000);

            int[] target = new int[n + offset];
            tree1.Keys.CopyTo (target, offset);

            for (int k = 0; k < n; ++k)
                Assert.AreEqual (k, target[k + offset]);
        }


        [TestMethod]
        public void UnitRdk_gICollectionCopyTo()
        {
            Setup();
            tree2.Add ("alpha", 1);
            tree2.Add ("beta", 2);
            tree2.Add ("gamma", 3);

            var target = new string[tree2.Count];

            genKeys2.CopyTo (target, 0);

            Assert.AreEqual ("alpha", target[0]);
            Assert.AreEqual ("beta", target[1]);
            Assert.AreEqual ("gamma", target[2]);
        }


        [TestMethod]
        [ExpectedException (typeof (NotSupportedException))]
        public void CrashRdk_gICollectionRemove_NotSupported()
        {
            Setup();
            genKeys2.Remove ("omega");
        }

        #endregion

        #region Test Keys bonus methods
#if ! TEST_BCL

        [TestMethod]
        public void UnitRdk_xIndexer()
        {
            var rd = new RankedDictionary<string,int>() { { "0zero", 0 }, { "1one", -1 }, { "2two", -2 } };
            var k1 = rd.Keys[1];

            Assert.AreEqual ("0zero",rd.Keys[0]);
            Assert.AreEqual ("1one", rd.Keys[1]);
            Assert.AreEqual ("2two", rd.Keys[2]);
        }


        [TestMethod]
        [ExpectedException (typeof (ArgumentOutOfRangeException))]
        public void CrashRdk_xElementAt_ArgumentOutOfRange1()
        {
            var rd = new RankedDictionary<int,int>();
            var k1 = rd.Keys.ElementAt (-1);
        }

        [TestMethod]
        [ExpectedException (typeof (ArgumentOutOfRangeException))]
        public void CrashRdk_xElementAt_ArgumentOutOfRange2()
        {
            var rd = new RankedDictionary<int,int>();
            var k1 = rd.Keys.ElementAt (0);
        }

        [TestMethod]
        public void UnitRdk_xElementAt()
        {
            var rd = new RankedDictionary<string,int>();
            rd.Add ("one", 1); rd.Add ("two", 2);
            var k1 = rd.Keys.ElementAt (1);

            Assert.AreEqual ("two", k1);
        }


        [TestMethod]
        public void UnitRdk_xElementAtOrDefault()
        {
            var rd = new RankedDictionary<string,int>();
            rd.Add ("one", 1); rd.Add ("two", 2);

            var kn = rd.Keys.ElementAtOrDefault (-1);
            var k1 = rd.Keys.ElementAtOrDefault (1);
            var k2 = rd.Keys.ElementAtOrDefault (2);

            Assert.AreEqual (default (String), kn);
            Assert.AreEqual ("two", k1);
            Assert.AreEqual (default (String), k2);
        }


        [TestMethod]
        [ExpectedException (typeof (ArgumentNullException))]
        public void CrashRdk_xIndexOf_ArgumentNull()
        {
            var rd = new RankedDictionary<string,int>();
            var k1 = rd.Keys.IndexOf (null);
        }

        [TestMethod]
        public void UnitRdk_xIndexOf()
        {
            var rd = new RankedDictionary<string,int>();
            rd.Add ("one", 1); rd.Add ("two", 2);
            var k1 = rd.Keys.IndexOf ("two");

            Assert.AreEqual (1, k1);
        }

#endif
        #endregion

        #region Test Keys enumeration

        [TestMethod]
        [ExpectedException (typeof (InvalidOperationException))]
        public void CrashRdk_oICollectionCurrent_InvalidOperation()
        {
            Setup();
            tree2.Add ("CC", 3);

            System.Collections.ICollection oKeys = objCol2.Keys;
            System.Collections.IEnumerator etor = oKeys.GetEnumerator();

            object cur = etor.Current;
        }

        [TestMethod]
        public void UnitRdk_GetEnumerator()
        {
            int n = 100;
            Setup (4);

            for (int k = 0; k < n; ++k)
                tree1.Add (k, k + 1000);

            int actualCount = 0;
            foreach (int key in tree1.Keys)
            {
                Assert.AreEqual (actualCount, key);
                ++actualCount;
            }

            Assert.AreEqual (n, actualCount);
        }

        [TestMethod]
        public void UnitRdk_gICollectionGetEnumerator()
        {
            int n = 10;
            Setup();

            for (int k = 0; k < n; ++k)
                tree2.Add (k.ToString(), k);

            int expected = 0;
            var etor = genKeys2.GetEnumerator();

            var rewoundKey = etor.Current;
            Assert.AreEqual (rewoundKey, default (string));

            while (etor.MoveNext())
            {
                var key = etor.Current;
                Assert.AreEqual (expected.ToString(), key);
                ++expected;
            }
            Assert.AreEqual (n, expected);
        }

        [TestMethod]
        [ExpectedException (typeof (InvalidOperationException))]
        public void CrashRdk_EtorHotUpdate()
        {
            Setup (4);
            tree2.Add ("vv", 1);
            tree2.Add ("mm", 2);
            tree2.Add ("qq", 3);

            int n = 0;
            foreach (var kv in tree2.Keys)
            {
                if (++n == 2)
                    tree2.Remove ("vv");
            }
        }

        #endregion


        #region Test Values constructor

        [TestMethod]
        [ExpectedException (typeof (ArgumentNullException))]
        public void CrashRdv_Ctor_ArgumentNull()
        {
            Setup();
#if TEST_BCL
            var vals = new SortedDictionary<int,int>.ValueCollection (null);
#else
            var vals = new RankedDictionary<int,int>.ValueCollection (null);
#endif
        }

        [TestMethod]
        public void UnitRdv_Ctor()
        {
            Setup();
            tree1.Add (1, -1);
#if TEST_BCL
            var vals = new SortedDictionary<int,int>.ValueCollection (tree1);
#else
            var vals = new RankedDictionary<int,int>.ValueCollection (tree1);
#endif
            Assert.AreEqual (1, vals.Count);
        }

        #endregion

        #region Test Values properties

        [TestMethod]
        public void UnitRdv_Count()
        {
            Setup();
            foreach (int key in iVals1)
                tree1.Add (key, key + 1000);

            Assert.AreEqual (iVals1.Length, tree1.Values.Count);
        }


        [TestMethod]
        public void UnitRdv_gICollectionIsReadonly()
        {
            Setup();
            var gc = (System.Collections.Generic.ICollection<int>) tree1.Values;
            Assert.IsTrue (gc.IsReadOnly);
        }


        [TestMethod]
        public void UnitRdv_oICollectionSyncRoot()
        {
            Setup();
            var oc = (System.Collections.ICollection) tree2.Values;
            Assert.IsFalse (oc.SyncRoot.GetType().IsValueType);
        }

        #endregion

        #region Test Values methods

        [TestMethod]
        [ExpectedException (typeof (NotSupportedException))]
        public void CrashRdv_gICollectionAdd_NotSupported()
        {
            Setup();
            genValues2.Add (9);
        }


        [TestMethod]
        [ExpectedException (typeof (NotSupportedException))]
        public void CrashRdv_gICollectionClear_NotSupported()
        {
            Setup();
            genValues2.Clear();
        }


        [TestMethod]
        public void UnitRdv_gICollectionContains()
        {
            Setup();
            tree2.Add ("alpha", 10);
            tree2.Add ("beta", 20);

            Assert.IsTrue (genValues2.Contains (20));
            Assert.IsFalse (genValues2.Contains (15));
        }


        [TestMethod]
        [ExpectedException (typeof (ArgumentNullException))]
        public void CrashRdv_CopyTo_ArgumentNull()
        {
            Setup();
            var target = new int[iVals1.Length];
            tree1.Values.CopyTo (null, -1);
        }

        [TestMethod]
        [ExpectedException (typeof (ArgumentOutOfRangeException))]
        public void CrashRdv_CopyTo_ArgumentOutOfRange()
        {
            Setup();
            var target = new int[10];
            tree1.Values.CopyTo (target, -1);
        }

        [TestMethod]
        [ExpectedException (typeof (ArgumentException))]
        public void CrashRdv_CopyTo_Argument()
        {
            Setup();

            for (int key = 1; key < 10; ++key)
                tree1.Add (key, key + 1000);

            var target = new int[4];
            tree1.Values.CopyTo (target, 2);
        }

        [TestMethod]
        public void UnitRdv_CopyTo()
        {
            int n = 10;
            int offset = 5;
            Setup();
            for (int k = 0; k < n; ++k)
                tree1.Add (k, k + 1000);

            int[] target = new int[n + offset];
            tree1.Values.CopyTo (target, offset);

            for (int k = 0; k < n; ++k)
                Assert.AreEqual (k + 1000, target[k + offset]);
        }


        [TestMethod]
        public void UnitRdv_gICollectionCopyTo()
        {
            Setup();
            tree2.Add ("alpha", 1);
            tree2.Add ("beta", 2);
            tree2.Add ("gamma", 3);

            var target = new int[tree2.Count];

            genValues2.CopyTo (target, 0);

            Assert.AreEqual (1, target[0]);
            Assert.AreEqual (2, target[1]);
            Assert.AreEqual (3, target[2]);
        }


        [TestMethod]
        [ExpectedException (typeof (NotSupportedException))]
        public void CrashRdv_gICollectionRemove_NotSupported()
        {
            Setup();
            genValues2.Remove (9);
        }

        #endregion

        #region Test Values enumeration

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CrashRdv_OICollectionCurrent_InvalidOperation()
        {
            Setup();
            tree2.Add ("CC", 3);

            System.Collections.ICollection oVals = objCol2.Values;
            System.Collections.IEnumerator etor = oVals.GetEnumerator();

            object cur = etor.Current;
        }

        [TestMethod]
        public void UnitRdv_GetEnumerator()
        {
            int n = 100;
            Setup();

            for (int k = 0; k < n; ++k)
                tree1.Add (k, k + 1000);

            int actualCount = 0;
            foreach (int value in tree1.Values)
            {
                Assert.AreEqual (actualCount + 1000, value);
                ++actualCount;
            }

            Assert.AreEqual (n, actualCount);
        }

        [TestMethod]
        public void UnitRdv_gICollectionGetEnumerator()
        {
            int n = 10;
            Setup();

            for (int k = 0; k < n; ++k)
                tree2.Add (k.ToString(), k);

            int expected = 0;
            var etor = genValues2.GetEnumerator();

            var rewoundVal = etor.Current;
            Assert.AreEqual (rewoundVal, default (int));

            while (etor.MoveNext())
            {
                var val = etor.Current;
                Assert.AreEqual (expected, val);
                ++expected;
            }
            Assert.AreEqual (n, expected);
        }

        [TestMethod]
        [ExpectedException (typeof (InvalidOperationException))]
        public void CrashRdv_EtorHotUpdate()
        {
            Setup (4);
            tree2.Add ("vv", 1);
            tree2.Add ("mm", 2);
            tree2.Add ("qq", 3);

            int n = 0;
            foreach (var kv in tree2.Keys)
            {
                if (++n == 2)
                    tree2.Clear();
            }
        }

        #endregion
    }
}
