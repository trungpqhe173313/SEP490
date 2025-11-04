using NB.Model.Entities;
using NB.Service.AccountService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.Core.JwtService
{
    public interface IJwtService
    {
        string GenerateToken(UserInfo user);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        bool ValidateRefreshToken(string refreshToken);
        DateTime GetAccessTokenExpiry();
        DateTime GetRefreshTokenExpiry();
    }
}
