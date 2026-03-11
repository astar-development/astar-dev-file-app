# Common Development Tasks

Step-by-step guides for common development tasks in the AStar Dev OneDrive Sync Client.

---

## Adding a New Service

<PROCESS_REQUIREMENTS type="MANDATORY">
All services must be interface-based for testability. Services are automatically registered via source generators using the `[Service]` attribute.
</PROCESS_REQUIREMENTS>

**Steps**:

1. **Create Interface**: `Infrastructure/Services/I<ServiceName>.cs`

   ```csharp
   public interface IMyService
   {
       Task<Result<Data>> ProcessAsync(string input, CancellationToken ct = default);
   }
   ```

2. **Implement Service**: `Infrastructure/Services/<ServiceName>.cs`

   ```csharp
   [Service(ServiceLifetime.Scoped, As = typeof(IMyService))]
   public class MyService : IMyService
   {
       private readonly IDependency _dependency;

       public MyService(IDependency dependency)
       {
           _dependency = dependency;
       }

       public async Task<Result<Data>> ProcessAsync(string input, CancellationToken ct = default)
       {
           // Implementation
       }
   }
   ```

3. **Add `[Service]` Attribute**: Choose appropriate lifetime:
   - `Singleton`: Stateless, shared across app lifetime
   - `Scoped`: Per-operation, owns resources (e.g., DbContext)
   - `Transient`: New instance per injection (rarely used)

4. **Inject in Consumers**: Via constructor parameters

   ```csharp
   public class Consumer
   {
       private readonly IMyService _myService;

       public Consumer(IMyService myService)
       {
           _myService = myService;
       }
   }
   ```

5. **Create Tests**: `test/.../MyServiceShould.cs`

   ```csharp
   public class MyServiceShould
   {
       private readonly Mock<IDependency> _mockDependency;
       private readonly IMyService _sut;

       public MyServiceShould()
       {
           _mockDependency = new Mock<IDependency>();
           _sut = new MyService(_mockDependency.Object);
       }

       [Fact]
       public async Task ProcessInputSuccessfully()
       {
           // Arrange
           _mockDependency.Setup(d => d.GetDataAsync(It.IsAny<string>()))
               .ReturnsAsync(new Data());

           // Act
           var result = await _sut.ProcessAsync("test");

           // Assert
           result.IsSuccess.Should().BeTrue();
       }
   }
   ```

6. **Mock in Tests**: Create test doubles via interface

---

## Adding a New Repository

<PROCESS_REQUIREMENTS type="MANDATORY">
Repositories abstract data access. One repository per aggregate/entity. Methods return domain models with `Result<T>` or `Option<T>`. DbContext is scoped to service lifetime.
</PROCESS_REQUIREMENTS>

**Steps**:

1. **Create Interface**: `Infrastructure/Repositories/I<EntityName>Repository.cs`

   ```csharp
   public interface IMyEntityRepository
   {
       Task<Option<MyEntity>> GetByIdAsync(string id, CancellationToken ct = default);
       Task<Result<MyEntity>> CreateAsync(MyEntity entity, CancellationToken ct = default);
       Task<Result<Unit>> UpdateAsync(MyEntity entity, CancellationToken ct = default);
       Task<Result<Unit>> DeleteAsync(string id, CancellationToken ct = default);
       Task<IReadOnlyList<MyEntity>> GetAllAsync(CancellationToken ct = default);
   }
   ```

2. **Implement Repository**: `Infrastructure/Repositories/<EntityName>Repository.cs`

   ```csharp
   [Service(ServiceLifetime.Scoped, As = typeof(IMyEntityRepository))]
   public class MyEntityRepository : IMyEntityRepository
   {
       private readonly SyncDbContext _context;

       public MyEntityRepository(SyncDbContext context)
       {
           _context = context;
       }

       public async Task<Option<MyEntity>> GetByIdAsync(string id, CancellationToken ct = default)
       {
           var entity = await _context.MyEntities
               .Include(e => e.RelatedData)  // Eager load relationships
               .AsNoTracking()               // Read-only optimization
               .FirstOrDefaultAsync(e => e.Id == id, ct);

           return entity != null ? Option<MyEntity>.Some(entity) : Option<MyEntity>.None();
       }
   }
   ```

3. **Create Entity**: In `Core/Data/Entities/MyEntity.cs`

   ```csharp
   public class MyEntity
   {
       public string Id { get; set; } = string.Empty;
       public string Name { get; set; } = string.Empty;
       public DateTime CreatedAt { get; set; }

       // Navigation properties
       public List<RelatedData> RelatedData { get; set; } = new();
   }
   ```

4. **Configure DbContext**: In `SyncDbContext.OnModelCreating()`

   ```csharp
   modelBuilder.Entity<MyEntity>(entity =>
   {
       entity.HasKey(e => e.Id);
       entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
       entity.HasIndex(e => e.Name);  // Add indexes for queries

       entity.HasMany(e => e.RelatedData)
           .WithOne()
           .HasForeignKey("MyEntityId")
           .OnDelete(DeleteBehavior.Cascade);
   });
   ```

5. **Create Migration**:

   ```bash
   dotnet ef migrations add AddMyEntity \
     --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
     --startup-project src/AStar.Dev.OneDrive.Sync.Client
   ```

6. **Apply Migration**:
   ```bash
   dotnet ef database update \
     --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
     --startup-project src/AStar.Dev.OneDrive.Sync.Client
   ```

---

## Adding a New ViewModel

<PROCESS_REQUIREMENTS type="MANDATORY">
ViewModels must inherit from `ReactiveObject` and use reactive properties. All business logic delegated to services. ViewModels are UI state coordinators, not business logic containers.
</PROCESS_REQUIREMENTS>

**Steps**:

1. **Create Class**: Inherit from `ReactiveObject`

   ```csharp
   public class MyViewModel : ReactiveObject
   {
       private readonly IMyService _myService;
       private readonly ObservableAsPropertyHelper<string> _status;

       public MyViewModel(IMyService myService)
       {
           _myService = myService;

           // Reactive property binding
           _status = this.WhenAnyValue(x => x.IsLoading)
               .Select(isLoading => isLoading ? "Loading..." : "Ready")
               .ToProperty(this, x => x.Status);
       }

       public string Status => _status.Value;
   }
   ```

2. **Add Properties**: Use reactive properties with `this.WhenAnyValue()`

   ```csharp
   private bool _isLoading;
   public bool IsLoading
   {
       get => _isLoading;
       set => this.RaiseAndSetIfChanged(ref _isLoading, value);
   }
   ```

3. **Create Commands**: Use `ReactiveCommand` with observables

   ```csharp
   public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }

   public MyViewModel(IMyService myService)
   {
       _myService = myService;

       // Can execute when not loading
       var canExecute = this.WhenAnyValue(x => x.IsLoading, isLoading => !isLoading);

       LoadDataCommand = ReactiveCommand.CreateFromTask(
           async () => await LoadDataAsync(),
           canExecute);
   }

   private async Task LoadDataAsync()
   {
       IsLoading = true;
       try
       {
           var result = await _myService.ProcessAsync("data");
           // Handle result
       }
       finally
       {
           IsLoading = false;
       }
   }
   ```

4. **Add Tests**: Mock dependencies via interfaces

   ```csharp
   public class MyViewModelShould
   {
       [Fact]
       public async Task LoadDataWhenCommandExecuted()
       {
           var mockService = new Mock<IMyService>();
           var viewModel = new MyViewModel(mockService.Object);

           await viewModel.LoadDataCommand.Execute();

           mockService.Verify(s => s.ProcessAsync(It.IsAny<string>()), Times.Once);
       }
   }
   ```

5. **Create View**: XAML with DataContext binding
   ```xml
   <UserControl xmlns="https://github.com/avaloniaui"
                xmlns:vm="using:AStar.Dev.OneDrive.Sync.Client.ViewModels"
                x:DataType="vm:MyViewModel">

       <StackPanel>
           <TextBlock Text="{Binding Status}" />
           <Button Content="Load Data" Command="{Binding LoadDataCommand}" />
       </StackPanel>
   </UserControl>
   ```

---

## Modifying Database Schema

<PROCESS_REQUIREMENTS type="MANDATORY">
All schema changes must go through EF Core migrations. Never modify the database directly. Always review generated migrations before applying. Test migrations on a copy of production data before deploying.
</PROCESS_REQUIREMENTS>

**Steps**:

1. **Edit Entity** in `Core/Data/Entities/`

   ```csharp
   public class MyEntity
   {
       // Add new property
       public string NewProperty { get; set; } = string.Empty;
   }
   ```

2. **Update DbContext** configuration if needed (in `SyncDbContext.OnModelCreating()`)

   ```csharp
   modelBuilder.Entity<MyEntity>(entity =>
   {
       entity.Property(e => e.NewProperty)
           .IsRequired()
           .HasMaxLength(100);
   });
   ```

3. **Create Migration**:

   ```bash
   dotnet ef migrations add AddNewPropertyToMyEntity \
     --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
     --startup-project src/AStar.Dev.OneDrive.Sync.Client
   ```

4. **Review Generated Migration**: Check `Infrastructure/Data/Migrations/`
   - Verify `Up()` method adds new column correctly
   - Verify `Down()` method removes column (for rollback)
   - Add data migrations if needed (e.g., populate new column with default values)

5. **Apply Migration**:

   ```bash
   dotnet ef database update \
     --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
     --startup-project src/AStar.Dev.OneDrive.Sync.Client
   ```

6. **Update Repository**: Add queries/methods for new properties
   ```csharp
   public async Task<IReadOnlyList<MyEntity>> GetByNewPropertyAsync(
       string newPropertyValue,
       CancellationToken ct = default)
   {
       return await _context.MyEntities
           .Where(e => e.NewProperty == newPropertyValue)
           .ToListAsync(ct);
   }
   ```

**Rolling Back a Migration**:

```bash
# List migrations
dotnet ef migrations list

# Revert to previous migration
dotnet ef database update PreviousMigrationName

# Remove migration file
dotnet ef migrations remove
```
