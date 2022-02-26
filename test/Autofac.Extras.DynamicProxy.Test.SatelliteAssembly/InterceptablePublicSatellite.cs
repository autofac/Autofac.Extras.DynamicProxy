// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.DynamicProxy.Test.SatelliteAssembly;

public class InterceptablePublicSatellite : IPublicInterfaceSatellite
{
    public string PublicMethod()
    {
        throw new NotImplementedException();
    }

    public string InternalMethod()
    {
        throw new NotImplementedException();
    }
}
