using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.Core.JwtService
{
    public interface IJwtService
    {
        //string GenerateToken(AppUser user);
        string GenerateRefreshToken();
        DateTime GetExpiration();
    }
}
