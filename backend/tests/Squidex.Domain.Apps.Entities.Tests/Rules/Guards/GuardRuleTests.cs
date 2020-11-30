﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Threading.Tasks;
using FakeItEasy;
using Squidex.Domain.Apps.Core.Rules;
using Squidex.Domain.Apps.Core.Rules.Triggers;
using Squidex.Domain.Apps.Core.TestHelpers;
using Squidex.Domain.Apps.Entities.Rules.Commands;
using Squidex.Domain.Apps.Entities.TestHelpers;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Collections;
using Squidex.Infrastructure.Validation;
using Xunit;

namespace Squidex.Domain.Apps.Entities.Rules.Guards
{
    public class GuardRuleTests : IClassFixture<TranslationsFixture>
    {
        private readonly Uri validUrl = new Uri("https://squidex.io");
        private readonly NamedId<DomainId> appId = NamedId.Of(DomainId.NewGuid(), "my-app");
        private readonly NamedId<DomainId> schemaId = NamedId.Of(DomainId.NewGuid(), "my-schema");
        private readonly IAppProvider appProvider = A.Fake<IAppProvider>();

        public sealed class TestAction : RuleAction
        {
            public Uri Url { get; set; }
        }

        public GuardRuleTests()
        {
            A.CallTo(() => appProvider.GetSchemaAsync(appId.Id, schemaId.Id, false, false))
                .Returns(Mocks.Schema(appId, schemaId));
        }

        [Fact]
        public async Task CanCreate_should_throw_exception_if_trigger_null()
        {
            var command = new CreateRule
            {
                Trigger = null!,
                Action = new TestAction
                {
                    Url = validUrl
                },
                AppId = appId
            };

            await ValidationAssert.ThrowsAsync(() => GuardRule.CanCreate(command, appProvider),
                new ValidationError("Trigger is required.", "Trigger"));
        }

        [Fact]
        public async Task CanCreate_should_throw_exception_if_action_null()
        {
            var command = new CreateRule
            {
                Trigger = new ContentChangedTriggerV2
                {
                    Schemas = ReadOnlyCollection.Empty<ContentChangedTriggerSchemaV2>()
                },
                Action = null!,
                AppId = appId
            };

            await ValidationAssert.ThrowsAsync(() => GuardRule.CanCreate(command, appProvider),
                new ValidationError("Action is required.", "Action"));
        }

        [Fact]
        public async Task CanCreate_should_not_throw_exception_if_trigger_and_action_valid()
        {
            var command = new CreateRule
            {
                Trigger = new ContentChangedTriggerV2
                {
                    Schemas = ReadOnlyCollection.Empty<ContentChangedTriggerSchemaV2>()
                },
                Action = new TestAction
                {
                    Url = validUrl
                },
                AppId = appId
            };

            await GuardRule.CanCreate(command, appProvider);
        }

        [Fact]
        public async Task CanUpdate_should_not_throw_exception_if_all_properties_are_null()
        {
            var command = new UpdateRule();

            await GuardRule.CanUpdate(command, Rule(), appProvider);
        }

        [Fact]
        public async Task CanUpdate_should_not_throw_exception_if_rule_has_already_this_name()
        {
            var command = new UpdateRule { Name = "MyName" };

            await GuardRule.CanUpdate(command, Rule(), appProvider);
        }

        [Fact]
        public async Task CanUpdate_should_not_throw_exception_if_trigger_action__and_name_are_valid()
        {
            var command = new UpdateRule
            {
                Trigger = new ContentChangedTriggerV2
                {
                    Schemas = ReadOnlyCollection.Empty<ContentChangedTriggerSchemaV2>()
                },
                Action = new TestAction
                {
                    Url = validUrl
                },
                Name = "NewName"
            };

            await GuardRule.CanUpdate(command, Rule(), appProvider);
        }

        [Fact]
        public void CanEnable_should_not_throw_exception()
        {
            var command = new EnableRule();

            GuardRule.CanEnable(command);
        }

        [Fact]
        public void CanDisable_should_not_throw_exception()
        {
            var command = new DisableRule();

            GuardRule.CanDisable(command);
        }

        [Fact]
        public void CanDelete_should_not_throw_exception()
        {
            var command = new DeleteRule();

            GuardRule.CanDelete(command);
        }

        private IRuleEntity Rule()
        {
            var rule = A.Fake<IRuleEntity>();

            A.CallTo(() => rule.AppId).Returns(appId);

            return rule;
        }
    }
}
