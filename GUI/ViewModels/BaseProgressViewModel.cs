﻿using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DivinityModManager.ViewModels
{
	public class BaseProgressViewModel : ReactiveObject
	{
		[Reactive] public bool CanRun { get; set; }
		[Reactive] public bool IsVisible { get; set; }
		[Reactive] public bool IsProgressActive { get; set; }
		[Reactive] public string ProgressTitle { get; set; }
		[Reactive] public string ProgressWorkText { get; set; }
		[Reactive] public double ProgressValue { get; set; }

		private readonly ObservableAsPropertyHelper<bool> _isRunning;
		/// <summary>
		/// True when the RunCommand is executing.
		/// </summary>
		public bool IsRunning => _isRunning.Value;

		public ReactiveCommand<Unit, bool> RunCommand { get; private set; }
		public ReactiveCommand<Unit, Unit> CancelRunCommand { get; private set; }
		public ReactiveCommand<Unit, Unit> CloseCommand { get; private set; }

		internal async Task<Unit> UpdateProgress(string title = "", string workText = "", double value = -1)
		{
			await Observable.Start(() =>
			{
				if (!String.IsNullOrEmpty(title))
				{
					ProgressTitle = title;
				}
				if (!String.IsNullOrEmpty(workText))
				{
					ProgressWorkText = workText;
				}
				if (value > -1)
				{
					ProgressValue = value;
				}
			}, RxApp.MainThreadScheduler);
			return Unit.Default;
		}

		public virtual async Task<bool> Run(CancellationToken cts)
		{
			return true;
		}

		public virtual void Close()
		{
			IsVisible = false;
			IsProgressActive = false;
		}

		public BaseProgressViewModel()
		{
			var canRun = this.WhenAnyValue(x => x.CanRun);
			RunCommand = ReactiveCommand.CreateFromObservable(() => Observable.StartAsync(cts => Run(cts)).TakeUntil(this.CancelRunCommand), canRun);
			CancelRunCommand = ReactiveCommand.Create(() => { }, this.RunCommand.IsExecuting);
			CloseCommand = ReactiveCommand.Create(Close, RunCommand.IsExecuting.Select(b => !b));

			_isRunning = this.RunCommand.IsExecuting.ToProperty(this, nameof(IsRunning), true, RxApp.MainThreadScheduler);
		}
	}
}
