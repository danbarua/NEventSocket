using System;
using System.Collections.Generic;
using System.Text;

namespace NEventSocket.Examples.NetCore
{
    using System.ComponentModel;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.Extensions.Logging;

    public class DisplayCommandLineTasks : ICommandLineTask
    {
        private readonly ILifetimeScope _container;
        private readonly ILogger _logger;
        private readonly CommandLineReader _commandLineReader;

        public DisplayCommandLineTasks(ILifetimeScope container, ILogger logger, CommandLineReader commandLineReader)
        {
            _container = container;
            _logger = logger;
            _commandLineReader = commandLineReader;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            var tasks = _container
                .ImplementationsFor<ICommandLineTask>()
                .Concat(_container.ImplementationsForGenericType(typeof(ICommandLineTask<>)))
                .Where(t => t != GetType())
                .ToArray();

            while (!cancellationToken.IsCancellationRequested)
            {
                for (var index = 1; index < tasks.Length + 1; index++)
                {
                    var taskType = tasks[index - 1];
                    _logger.Information("{0}. {1}", index, taskType.Name);
                }

                _logger.Information("Enter a number of a task or empty to exit");

                var readLine = Console.ReadLine();

                if (string.IsNullOrEmpty(readLine))
                    return;

                int taskIndex;
                try
                {
                    taskIndex = readLine.To<int>() - 1;
                    if (tasks.Length <= taskIndex || taskIndex < 0)
                    {
                        _logger.Error("Invalid task number");
                        continue;
                    }
                }
                catch (FormatException e)
                {
                    _logger.Error(e, "");
                    continue;
                }

                var type = tasks[taskIndex];

                using (var scope = _container.BeginLifetimeScope("task"))
                {
                    var task = scope.Resolve(type);
                    try
                    {
                        await RunTask(cancellationToken, task);

                        _logger.Information("Succesfully ran task {task}", task);
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.Warning("Task canceled by user {task}", task);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Warning("Operation canceled by user {task}", task);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to execute task {task}", task);
                    }
                }
                Console.WriteLine();
            }
        }

        private async Task RunTask(CancellationToken cancellationToken, object task)
        {
            Type concreteType;
            var parameterlessTask = task as ICommandLineTask;
            if (parameterlessTask != null)
                await parameterlessTask.Run(cancellationToken);
            else if (task.GetType().IsOfGenericType(typeof(ICommandLineTask<>), out concreteType))
            {
                var paramType = concreteType.GetGenericArguments()[0];
                var methodInfo = task.GetType().GetMethod("Run", new[] { paramType, typeof(CancellationToken) });
                var parameterInfo = methodInfo.GetParameters()[0];
                var parameterName = parameterInfo.GetCustomAttribute<DescriptionAttribute>()
                    .Get(d => d.Description, parameterInfo.Name);
                var parameters = _commandLineReader.ReadObject(paramType, cancellationToken, parameterName);
                await (Task)methodInfo.Invoke(task, new[] { parameters, cancellationToken });
            }
        }
    }
}
