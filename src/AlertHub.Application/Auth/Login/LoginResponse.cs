using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Auth.Login;

public record LoginResponse(string AccessToken, string RefreshToken);
