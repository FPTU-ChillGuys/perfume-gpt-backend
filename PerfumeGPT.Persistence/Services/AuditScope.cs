using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Persistence.Services
{
	public class AuditScope : IAuditScope
	{
		private readonly AsyncLocal<bool> _isSystemAction = new();

		public bool IsSystemAction => _isSystemAction.Value;

		public IDisposable BeginSystemAction()
		{
			_isSystemAction.Value = true;
			return new SystemActionScope(() => _isSystemAction.Value = false);
		}

		private class SystemActionScope : IDisposable
		{
			private readonly Action _onDispose;
			private bool _disposed;

			public SystemActionScope(Action onDispose)
			{
				_onDispose = onDispose;
			}

			public void Dispose()
			{
				if (!_disposed)
				{
					_onDispose?.Invoke();
					_disposed = true;
				}
			}
		}
	}
}
