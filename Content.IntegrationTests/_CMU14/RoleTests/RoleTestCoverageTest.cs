using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RuMC14.RoleTests;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;

namespace Content.IntegrationTests._CMU14.RoleTests;

public sealed class RoleTestCoverageTest
{
    [Test]
    public async Task PersonalizationJobsHaveQuestionPools()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var prototypes = server.ProtoMan;

        await server.WaitAssertion(() =>
        {
            var missing = new SortedSet<string>();
            var insufficient = new SortedSet<string>();
            var questions = prototypes.EnumeratePrototypes<RoleTestQuestionPrototype>().ToList();

            foreach (var department in prototypes.EnumeratePrototypes<DepartmentPrototype>())
            {
                if (department.EditorHidden)
                    continue;

                foreach (var jobId in department.Roles)
                {
                    if (!prototypes.TryIndex<JobPrototype>(jobId, out var job) ||
                        !job.ID.StartsWith("AU14Job") ||
                        !job.SetPreference ||
                        job.Hidden ||
                        RoleTestShared.IsRoleTestExempt(job))
                    {
                        continue;
                    }

                    if (!prototypes.TryIndex<RoleTestQuestionPoolPrototype>(job.ID, out var pool))
                    {
                        missing.Add(job.ID);
                        continue;
                    }

                    var responsibility = RoleTestShared.GetResponsibility(job);
                    var required = RoleTestShared.GetRequiredRoleQuestionCount(
                        responsibility,
                        RoleTestShared.RequiresLaw(job));
                    var available = questions.Count(question => question.Pools.Contains(pool.Pool));

                    if (available < required)
                        insufficient.Add($"{job.ID}: pool {pool.Pool} has {available}, requires {required}");
                }
            }

            Assert.That(missing, Is.Empty,
                $"CMU personalization jobs without role test pools:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
            Assert.That(insufficient, Is.Empty,
                $"CMU personalization jobs without enough role questions:{Environment.NewLine}{string.Join(Environment.NewLine, insufficient)}");
        });

        await pair.CleanReturnAsync();
    }
}
