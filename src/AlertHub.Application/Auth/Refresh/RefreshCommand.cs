using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Auth.Refresh;

public record RefreshCommand(string AccessToken, string RefreshToken);
