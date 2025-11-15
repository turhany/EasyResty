using Resty.Models;

namespace Resty.Abstract
{
    public interface IEasyRestyClient
    {
        Task<EasyRestyClientResponse> RequestAsync(EasyRestyClientRequest request, CancellationToken cancellationToken);

        Task<EasyRestyClientResponse<T>> RequestAsync<T>(EasyRestyClientRequest request, CancellationToken cancellationToken);

        Task<EasyRestyClientResponse<TSuccessModel, TFailModel>> RequestAsync<TSuccessModel, TFailModel>(EasyRestyClientRequest request, CancellationToken cancellationToken);
    }
}
