// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using System.Runtime.CompilerServices;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.FileProviders;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Microsoft.Graph;
// using ZFHandler.Helpers;
// using ZFHandler.Mdtr.Handlers;
// using ZFHandler.Models;
// using ZFHandler.Models.ConfigurationSchemas;
// using ZFHandler.Models.ConfigurationSchemas.ClientConfigs.OneDrive;
//
// namespace ZFHandler.Services.BaseProviderImplementations.UploadServices
// {
//     public class OnedriveUserProvider :  BaseGraphClientProvider<OD_UserClientConfig>
//     {
//         private readonly ILogger<OnedriveUserProvider> _logger;
//         private readonly Dictionary<string, UploadTargetConfig> _targetConfigs;
//         private readonly Dictionary<string, GraphServiceClient> _clients = new();
//         private readonly IOptionsSnapshot<UploadTargetConfig> _optionsSnapshot;
//
//         public OnedriveUserProvider(ILogger<OnedriveUserProvider> logger, IOptions<UploadTargetConfig[]> options, IOptionsSnapshot<UploadTargetConfig> optionsSnapshot)
//         {
//             _logger = logger;
//             _optionsSnapshot = optionsSnapshot;
//             _targetConfigs = options.Value.Where(x => x.Type == JobType.OnedriveUser).ToDictionary(uploadTargetConfig =>
//                 uploadTargetConfig.Identifier ?? throw new NullReferenceException(uploadTargetConfig.Identifier));
//         }
//
//         private GraphServiceClient GetTargetClient(string name)
//         {
//             if (!_clients.ContainsKey(name))
//                 _clients.Add(name, new GraphServiceClient(OnedriveHelpers.DoAuth(
//                     new OD_UserClientConfig(_targetConfigs[name]?.ClientConfig ??
//                                           throw new IndexOutOfRangeException(
//                                               $"Error creating GraphServiceClient: {name} not found in in target configs")))));
//             return _clients[name];
//         }
//
//
//         /// <summary>
//         /// Given a source File to upload and a collection of targets,
//         /// applies any applicable name formatting and starts the upload process
//         /// </summary>
//         /// <param name="uploadTargets"></param>
//         /// <param name="sourceFileInfo"></param>
//         /// <param name="token"></param>
//         /// <returns></returns>
//         public async IAsyncEnumerable<UploadResult<DriveItem>> UploadForTargetsAsync(UploadTarget[] uploadTargets,
//             IFileInfo sourceFileInfo,
//             [EnumeratorCancellation] CancellationToken token = default)
//         {
//             foreach (var uploadTarget in uploadTargets)
//             {
//                 var name = string.IsNullOrWhiteSpace(uploadTarget.NamingTemplate)
//                     ? sourceFileInfo.Name
//                     : string.Format(uploadTarget.NamingTemplate, sourceFileInfo.Name);
//
//                 var relativePath = uploadTarget.RelativeRootUploadPath;
//                 string itemPath =
//                     $"/{relativePath}/{sourceFileInfo.Name}";
//
//
//                 yield return await UploadFileForProviderAsync(uploadTarget.ConfigId ?? "", sourceFileInfo,
//                     uploadTarget.RelativeRootUploadPath, token);
//             }
//         }
//
//         public override async Task Handle(UploadJobSpec<OD_UserClientConfig> notification, CancellationToken cancellationToken)
//         {
//             throw new NotImplementedException();
//         }
//     }
// }