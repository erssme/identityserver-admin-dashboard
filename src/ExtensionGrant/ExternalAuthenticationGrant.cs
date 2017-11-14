﻿using IdentityServer4.Models;
using IdentityServer4.Validation;
using IdentityServer.Data.Entities;
using IdentityServer.Helpers;
using IdentityServer.Interfaces.Processors;
using IdentityServer.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer.Interfaces.ExternalProviders;
using IdentityServer.Repositories.UnitOfWork;
using IdentityServer.Data;

namespace IdentityServer.ExtensionGrant
{
    public class ExternalAuthenticationGrant : IExtensionGrantValidator
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork<DatabaseContext> _unitOfWork;
        private readonly IExternalUserRepository _externalUserRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly IFacebookAuthProvider _facebookAuthProvider;
        private readonly IGoogleAuthProvider _googleAuthProvider;
        private readonly ITwitterAuthProvider _twitterAuthProvider;
        private readonly ILinkedInAuthProvider _linkedAuthProvider;
        private readonly INonEmailUserProcessor _nonEmailUserProcessor;
        private readonly IEmailUserProcessor _emailUserProcessor;
        public ExternalAuthenticationGrant(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork<DatabaseContext> unitOfWork,
        IExternalUserRepository externalUserRepository,
            IProviderRepository providerRepository,
            IFacebookAuthProvider facebookAuthProvider,
            IGoogleAuthProvider googleAuthProvider,
            ITwitterAuthProvider twitterAuthProvider,
            ILinkedInAuthProvider linkeInAuthProvider,
            INonEmailUserProcessor nonEmailUserProcessor,
            IEmailUserProcessor emailUserProcessor            
            )
        {
            _userManager = userManager                       ?? throw new ArgumentNullException(nameof(userManager));
            _unitOfWork = unitOfWork                         ?? throw new ArgumentNullException(nameof(unitOfWork));
            _externalUserRepository = externalUserRepository ?? throw new ArgumentNullException(nameof(externalUserRepository));
            _providerRepository = providerRepository         ?? throw new ArgumentNullException(nameof(providerRepository));
            _facebookAuthProvider = facebookAuthProvider     ?? throw new ArgumentNullException(nameof(facebookAuthProvider));
            _googleAuthProvider = googleAuthProvider         ?? throw new ArgumentNullException(nameof(googleAuthProvider));
            _twitterAuthProvider = twitterAuthProvider       ?? throw new ArgumentNullException(nameof(twitterAuthProvider));
            _linkedAuthProvider = linkeInAuthProvider        ?? throw new ArgumentNullException(nameof(linkeInAuthProvider));
            _nonEmailUserProcessor = nonEmailUserProcessor   ?? throw new ArgumentNullException(nameof(nonEmailUserProcessor));
            _emailUserProcessor = emailUserProcessor         ?? throw new ArgumentNullException(nameof(nonEmailUserProcessor));

            providers = new Dictionary<ProviderType, IExternalAuthProvider>();
            providers.Add(ProviderType.Facebook, _facebookAuthProvider);
            providers.Add(ProviderType.Google, _googleAuthProvider);
            providers.Add(ProviderType.Twitter, _twitterAuthProvider);
            providers.Add(ProviderType.LinkedIn, _linkedAuthProvider);
        }


        private Dictionary<ProviderType, IExternalAuthProvider> providers;
        
        public string GrantType => "external";
       


        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var provider = context.Request.Raw.Get("provider");
            if (string.IsNullOrWhiteSpace(provider))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "invalid provider");
                return;
            }

            
            var token = context.Request.Raw.Get("external_token");
            if(string.IsNullOrWhiteSpace(token))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "invalid external token");
                return;
            }

            var request_email = context.Request.Raw.Get("email"); 

            ProviderType providerType=(ProviderType)Enum.Parse(typeof(ProviderType), provider,true);

            if (!Enum.IsDefined(typeof(ProviderType), providerType))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "invalid provider");
                return;
            }

            var userInfo = providers[providerType].GetUserInfo(token);

            if(userInfo == null)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "couldn't retrieve user info from specified provider, please make sure that access token is not expired.");
                return;
            }
          
            if (string.IsNullOrWhiteSpace(request_email))
            {
                context.Result = await _nonEmailUserProcessor.Process(userInfo, provider);
                return;
            }

            context.Result = await _emailUserProcessor.Process(userInfo, request_email, provider);
            return;
        }
    }
}
