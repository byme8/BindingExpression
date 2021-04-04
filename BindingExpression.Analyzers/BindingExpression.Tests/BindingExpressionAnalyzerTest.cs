using System.Linq;
using System.Threading.Tasks;
using BindingExpression.Analyzers;
using BindingExpression.Analyzers.Utils;
using BindingExpression.Test.Data;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BindingExpression.Tests
{
    //ListView(() => true);
    //ListView(() => viewModel.Items);
    //ListView(() => viewModel.ToString());
    //ListView(() => viewModel);

    [TestClass]
    public class BindingExpressionAnalyzerTest
    {
        [TestMethod]
        public async Task LiteralBindingExpressionsNotAllowed()
        {
            var project = await TestProject.Project
                .ReplacePartOfDocumentAsync("Program.cs", "// line to replace", "ListView(() => true);");

            var diagnosticts = await project.ApplyAnalyzer(new BindingExpressionAnalyzer());
            var wrongBindingExpression = diagnosticts
                .FirstOrDefault(o => o.Id == BindingExpressionAnalyzer.BindingExpressionAnalyzerDescriptionId);

            Assert.IsNotNull(wrongBindingExpression);
        }
        
        [TestMethod]
        public async Task PropertyAccessBindingExpressionsAllowed()
        {
            var project = await TestProject.Project
                .ReplacePartOfDocumentAsync("Program.cs", "// line to replace", "ListView(() => viewModel.Items);");

            var diagnosticts = await project.ApplyAnalyzer(new BindingExpressionAnalyzer());
            var wrongBindingExpression = diagnosticts
                .FirstOrDefault(o => o.Id == BindingExpressionAnalyzer.BindingExpressionAnalyzerDescriptionId);

            Assert.IsNull(wrongBindingExpression);
        }
        
        [TestMethod]
        public async Task MethodBindingExpressionsNotAllowed()
        {
            var project = await TestProject.Project
                .ReplacePartOfDocumentAsync("Program.cs", "// line to replace", "ListView(() => viewModel.ToString());");

            var diagnosticts = await project.ApplyAnalyzer(new BindingExpressionAnalyzer());
            var wrongBindingExpression = diagnosticts
                .FirstOrDefault(o => o.Id == BindingExpressionAnalyzer.BindingExpressionAnalyzerDescriptionId);

            Assert.IsNotNull(wrongBindingExpression);
        }
        
        [TestMethod]
        public async Task DeepMethodBindingExpressionsNotAllowed()
        {
            var project = await TestProject.Project
                .ReplacePartOfDocumentAsync("Program.cs", "// line to replace", "ListView(() => viewModel.Items.ToString());");

            var diagnosticts = await project.ApplyAnalyzer(new BindingExpressionAnalyzer());
            var wrongBindingExpression = diagnosticts
                .FirstOrDefault(o => o.Id == BindingExpressionAnalyzer.BindingExpressionAnalyzerDescriptionId);

            Assert.IsNotNull(wrongBindingExpression);
        }
    }
}