﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Diogo Lucas">
//
// Copyright (C) 2010 Diogo Lucas
//
// This file is part of Moo.
//
// Moo is free software: you can redistribute it and/or modify
// it under the +terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along Moo.  If not, see http://www.gnu.org/licenses/.
// </copyright>
// <summary>
// Moo is a object-to-object multi-mapper.
// Email: diogo.lucas@gmail.com
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Moo.Tests.Initialization
{
    using FakeItEasy;
    using Moo;
    using Moo.Initialization;
    using NUnit.Framework;
    using Shouldly;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [TestFixture]
    public class MappingInitializerExtenderTest
    {
        [Test]
        public void InitializeMappings_NullInitializers_Throws()
        {
            var fakeMapper = A.Fake<IMappingRepository>();
            Should.Throw<ArgumentNullException>(
                () => fakeMapper.InitializeMappings(initializers: null));
        }

        [Test]
        public void InitializeMappings_WithInitializers_CallsInitializers()
        {
            var fakeRepo = A.Fake<IMappingRepository>();
            var fakeInitializers = A.CollectionOfFake<IMappingInitializer>(5);
            
            fakeRepo.InitializeMappings(fakeInitializers);

            foreach (var i in fakeInitializers)
            {
                A.CallTo(() => i.InitializeMappings(fakeRepo)).MustHaveHappened();
            }
        }

        [Test]
        public void InitializeMappings_NullAssemblies_Throws()
        {
            var fakeMapper = A.Fake<IMappingRepository>();
            Should.Throw<ArgumentNullException>(
                () => fakeMapper.InitializeMappings(assemblies: null));
        }
    }
}
