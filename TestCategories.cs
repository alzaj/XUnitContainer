using Xunit;
using Xunit.Extensions;
using Xunit.Sdk;
using System.Collections.Generic;
using System;
using System.Reflection;

//http://mariangemarcano.blogspot.de/2010/12/xunitnet-running-tests-testcategory.html
//xUnit.NET Extension to run the test that belongs to an specific category
//example of use:
//
//[CustomTestClassCommand]
//[TestCategory("AllMethodsInTheClass")]
//public class ExampleTestClass
//{
//    [Fact]
//    [TestCategory("Integration")]
//    [TestCategory("Attention")]
//    public void Index_GET()
//    {
//            Assert.Equal(true, makeLongRunningCalculations);
//    }

//    [Fact]
//    [TestCategory("Unit")]
//    public void Index_GET()
//    {
//        Assert.Equal(true, quickCalculation);
//    }
//}


namespace Xunit.Extensions
{
    public class TestCategoryAttribute : TraitAttribute
    {
        public TestCategoryAttribute(string category)
            : base("TestCategory", category) { }
    }

    public class CustomTestClassCommandAttribute : RunWithAttribute
    {
        public CustomTestClassCommandAttribute() : base(typeof(CustomTestClassCommand)) { }
    }

    public class CustomTestClassCommand : ITestClassCommand
    {
        // Delegate most of the work to the existing TestClassCommand class so that we
        // can preserve any existing behavior (like supporting IUseFixture<T>).
        readonly TestClassCommand cmd = new TestClassCommand();

        #region ITestClassCommand Members
        public object ObjectUnderTest
        {
            get { return cmd.ObjectUnderTest; }
        }

        public ITypeInfo TypeUnderTest
        {
            get { return cmd.TypeUnderTest; }
            set { cmd.TypeUnderTest = value; }
        }

        public int ChooseNextTest(ICollection<IMethodInfo> testsLeftToRun)
        {
            return cmd.ChooseNextTest(testsLeftToRun);
        }

        public Exception ClassFinish()
        {
            return cmd.ClassFinish();
        }

        public Exception ClassStart()
        {
            return cmd.ClassStart();
        }

        public IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo testMethod)
        {
            return cmd.EnumerateTestCommands(testMethod);
        }

        public bool IsTestMethod(IMethodInfo testMethod)
        {
            return cmd.IsTestMethod(testMethod);
        }

        public IEnumerable<IMethodInfo> EnumerateTestMethods()
        {
            string category;
            IAttributeInfo sampleTraitAttrib = null;
            List<IMethodInfo> uncategorisedMethods = new List<IMethodInfo>();

            foreach (IMethodInfo method in cmd.EnumerateTestMethods())
            {
                category = string.Empty;

                foreach (IAttributeInfo attr in method.GetCustomAttributes(typeof(TestCategoryAttribute)))
                {
                    if (sampleTraitAttrib == null)
                    {
                        sampleTraitAttrib = attr;
                    }
                    category = attr.GetPropertyValue<string>("Value");
                }

                if (!String.IsNullOrEmpty(category))
                {
                    yield return method;
                }
                else
                {
                    uncategorisedMethods.Add(method);
                }
                
            }

            if (sampleTraitAttrib != null)
            {
                foreach (IMethodInfo method in uncategorisedMethods) 
                {
                    yield return new MethodInfoWithOnlyDefaultCustomAttribute(method, sampleTraitAttrib);
                }
            }
            else
            {
                foreach (IMethodInfo method in uncategorisedMethods)
                {
                    yield return method;
                }
            }

        }
        #endregion
    }

    public class MethodInfoWithOnlyDefaultCustomAttribute : IMethodInfo 
    {
        //alzaj. This is my solution for methods not having TestCategory attribute.
        // This methods should show under cathegory "Uncategorised".
        // I wrap around original IMethodInfo and modify the GetCustomAttributes method.

        private IMethodInfo _originalMethodInfo;
        private IAttributeInfo _sampleTraitAttrib;
        private TestCategoryAttribute _attributeInstance;

        public MethodInfoWithOnlyDefaultCustomAttribute(IMethodInfo origMethodInfo, IAttributeInfo sampleTraitAttrib)
        {
            _originalMethodInfo = origMethodInfo;
            _sampleTraitAttrib = sampleTraitAttrib;
        }

        #region IMethodInfo Members
        public ITypeInfo Class { get { return _originalMethodInfo.Class; }}
        public bool IsAbstract { get { return _originalMethodInfo.IsAbstract; }}
        public bool IsStatic { get { return _originalMethodInfo.IsStatic; } }
        public MethodInfo MethodInfo { get { return _originalMethodInfo.MethodInfo; } }
        public string Name { get { return _originalMethodInfo.Name; } }
        public string ReturnType { get { return _originalMethodInfo.ReturnType; } }
        public string TypeName { get { return _originalMethodInfo.TypeName; } }
        
        public object CreateInstance()
        {
            return _originalMethodInfo.CreateInstance();
        }
        
        public IEnumerable<IAttributeInfo> GetCustomAttributes(Type attributeType)
        {
            //
            if (attributeType == typeof(TraitAttribute))
            {
                yield return new AttributeInfoWithDefaultVAlue(_sampleTraitAttrib);
            }
            else
            {
                foreach (IAttributeInfo attr in _originalMethodInfo.GetCustomAttributes(attributeType))
                  yield return attr;
            }
        }

        public bool HasAttribute(Type attributeType)
        {
            return _originalMethodInfo.HasAttribute(attributeType);
        }

        public void Invoke(object testClass, params object[] parameters)
        {
            _originalMethodInfo.Invoke(testClass, parameters);
        }

        #endregion //IMethodInfo Members
    }

    public class AttributeInfoWithDefaultVAlue : IAttributeInfo
    {
        private IAttributeInfo _originalAttribute;

        public AttributeInfoWithDefaultVAlue(IAttributeInfo originalAttribute)
        {
            //_originalAttribute.GetInstance(

        }

        #region IAttributeInfo Members
            public T GetInstance<T>() where T : Attribute
            {
                return (T)_originalAttribute.GetInstance<T>();
            }

            public TValue GetPropertyValue<TValue>(string propertyName)
            {
                return (TValue)Convert.ChangeType("Uncategorised", typeof(TValue));  
            }
        #endregion //IAttributeInfo Members
    }
}