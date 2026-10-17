using System;
using VeggieLife.BLL.Common.Constants;

namespace VeggieLife.API.Authorization;

/// <summary>
/// Alias cho MemberOnlyAttribute để tương thích ngược với các controller hiện hữu (FN-AUTH-03).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireMemberAttribute : MemberOnlyAttribute
{
    public RequireMemberAttribute(MemberFeatureType featureType = MemberFeatureType.General) : base(featureType)
    {
    }
}
