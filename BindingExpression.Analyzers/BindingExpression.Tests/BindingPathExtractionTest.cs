using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BindingExpression.Tests
{
    [TestClass]
    public class BindingPathExtractionTest
    {
        private ViewModel viewModel;

        class ViewModel
        {
            public int[] Items { get; set; }
            public DateTimeOffset Date { get; set; }
        }

        public BindingPathExtractionTest()
        {
            this.viewModel = new ViewModel();
        }

        string GetPath<TValue>(Expression<Func<TValue>> expression)
        {
            return expression.GetBindingPath();
        }
        
        
        [TestMethod]
        public void WorksForPropety()
        {
            Assert.AreEqual("Items", GetPath(() => viewModel.Items));
        }
        
        [TestMethod]
        public void WorksForDeepProperty()
        {
            Assert.AreEqual("Date.Day", GetPath(() => viewModel.Date.Day));
        }
        
        [TestMethod]
        public void WorksForConstant()
        {
            Assert.AreEqual(".", GetPath(() => viewModel));
        }
        
        [TestMethod]
        public void DoesntWorkForMethods()
        {
            Assert.ThrowsException<NotSupportedException>(() => GetPath(() => viewModel.ToString()));
        }
    }
}