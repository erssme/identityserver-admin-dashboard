﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using IdentityServer.Repositories.Interfaces;
using IdentityServer.Data.Entities;
using IdentityServer.Helpers;
using System.Net.Http;
using IdentityServer.Interfaces.ExternalProviders;

namespace IdentityServer.Providers
{
    public class FacebookAuthProvider : IFacebookAuthProvider
    {
        private readonly IExternalUserRepository _externalUserRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly HttpClient _httpClient;
        public FacebookAuthProvider(
            IExternalUserRepository externalUserRepository,
            IProviderRepository providerRepository,
            HttpClient httpClient
            )
        {
            _externalUserRepository = externalUserRepository;
            _providerRepository = providerRepository;
            _httpClient = httpClient;
        }

        public Provider Provider => _providerRepository.Get()
                                    .FirstOrDefault(x => x.Name.ToLower() == ProviderType.Facebook.ToString().ToLower());

        public JObject GetUserInfo(string accessToken)
        {
            if(Provider == null)
            {
                throw new ArgumentNullException(nameof(Provider));
            }

            var request = new Dictionary<string, string>();

            request.Add("fields", "id,email,name,gender,birthday");
            request.Add("access_token", accessToken);

            var result = _httpClient.GetAsync(Provider.UserInfoEndPoint + QueryBuilder.GetQuery(request, ProviderType.Facebook)).Result;
            if (result.IsSuccessStatusCode)
            {
                var infoObject = JObject.Parse(result.Content.ReadAsStringAsync().Result);
                return infoObject;
            }
            return null;
        }
    }
}
