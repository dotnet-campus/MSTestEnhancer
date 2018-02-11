using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest.Extensions.Core
{
    /// <inheritdoc />
    /// <summary>
    /// A contract based test case that is discovered from a unit test method.
    /// </summary>
    internal class ContractTestCase : ITestCase
    {
        /// <summary>
        /// Create a new instance of <see cref="ContractTestCase"/> with contract description and an action.
        /// </summary>
        /// <param name="contract">The description of a test contract.</param>
        /// <param name="testCase">The action of the which is used to test the contract.</param>
        internal ContractTestCase([NotNull] string contract, [NotNull] Action testCase)
        {
            if (testCase == null) throw new ArgumentNullException(nameof(testCase));
            _contract = contract ?? throw new ArgumentNullException(nameof(contract));
            _testCase = () =>
            {
                testCase();
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Create a new instance of <see cref="ContractTestCase"/> with contract description and an async action.
        /// </summary>
        /// <param name="contract">The description of a test contract.</param>
        /// <param name="testCase">The action of the which is used to test the contract.</param>
        internal ContractTestCase([NotNull] string contract, [NotNull] Func<Task> testCase)
        {
            _contract = contract ?? throw new ArgumentNullException(nameof(contract));
            _testCase = testCase ?? throw new ArgumentNullException(nameof(testCase));
        }

        /// <inheritdoc />
        public TestResult Result => _result ?? (_result = Execute());

        /// <summary>
        /// Invoke the test case action to get the test result.
        /// </summary>
        /// <returns>The test result of this test case.</returns>
        [NotNull]
        private TestResult Execute()
        {
            // Prepare the execution.
            Exception exception = null;
            var watch = new Stopwatch();
            watch.Start();

            // Execute the test case.
            try
            {
                _testCase.Invoke().Wait();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                watch.Stop();
            }

            // Return the test result.
            var result = new TestResult
            {
                DisplayName = _contract,
                Duration = watch.Elapsed,
            };
            if (exception == null)
            {
                result.Outcome = UnitTestOutcome.Passed;
            }
            else
            {
                result.Outcome = UnitTestOutcome.Failed;
                result.TestFailureException = exception;
            }

            return result;
        }

        [CanBeNull, ContractPublicPropertyName(nameof(Result))]
        private TestResult _result;

        /// <summary>
        /// Gets the action of this test case. Invoke this func will execute the test case.
        /// The returning value of this func will never be null, but it does not mean that it is an async func.
        /// </summary>
        [NotNull] private readonly Func<Task> _testCase;

        /// <summary>
        /// Gets the contract description of this test case.
        /// </summary>
        [NotNull] private readonly string _contract;
    }
}
