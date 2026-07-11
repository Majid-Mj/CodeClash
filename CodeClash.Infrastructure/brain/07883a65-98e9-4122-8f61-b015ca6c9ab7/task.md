# Dynamic Execution Engine Migration Tasks

## Component 1: Domain Layer
- [ ] Create `ProblemLanguageTemplate.cs` entity
- [ ] Modify `Problem.cs` to add LanguageTemplates collection and domain method

## Component 2: Infrastructure / Persistence
- [ ] Create `ProblemLanguageTemplateConfiguration.cs` EF config
- [ ] Modify `ProblemConfiguration.cs` to add relationship mapping
- [ ] Modify `ApplicationDbContext.cs` to expose DbSet

## Component 3: Application / Handler Layer
- [ ] Modify `IDockerExecutionService.cs` interface (wrapperTemplate instead of problemSlug)
- [ ] Modify `RunCodeCommandHandler.cs` to load template and pass to service
- [ ] Modify `CreateSubmissionCommandHandler.cs` to load template and pass to service

## Component 4: Infrastructure / Services & Seeding
- [ ] Rewrite `DockerExecutionService.cs` (remove all hardcoded slug logic, use wrapperTemplate)
- [ ] Modify `Program.cs` to seed 25 templates for all existing problems

## Component 5: Database Migration
- [ ] Run `dotnet ef migrations add AddProblemLanguageTemplates`
- [ ] Apply migration with `dotnet ef database update`

## Verification
- [ ] Build entire solution
- [ ] Query DB to verify templates are seeded
