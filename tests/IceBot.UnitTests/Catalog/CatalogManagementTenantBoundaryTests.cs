using Application.Catalog.Abstractions;
using Application.Catalog.Products.Commands;
using Application.Catalog.Products.Requests;
using Application.Catalog.Products.Queries;
using Application.Identity.Tokens.Claims;
using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Commands;
using Application.SalesCatalog.Menus.Requests;
using Application.Shared.Ownership;
using Application.Shared.Concurrency;
using Application.Tenants;
using Domain.Catalog.Entities;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Enums;
using NSubstitute;

namespace IceBot.UnitTests.Catalog;

public sealed class CatalogManagementTenantBoundaryTests
{
    [Fact]
    public void OrgAdmin_CanManageCatalogOnlyInsideAssignedOrganization()
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var context = OrgAdmin(organizationId);

        Assert.True(ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.ProductsManage, context, organizationId, null, null));
        Assert.True(ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.MenusManage, context, organizationId, null, null));
        Assert.False(ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.ProductsManage, context, otherOrganizationId, null, null));
        Assert.False(ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.MenusManage, context, otherOrganizationId, null, null));
    }

    [Fact]
    public void StoreRole_DoesNotExpandToOrganizationScope()
    {
        var organizationId = Guid.NewGuid();
        var assignedStoreId = Guid.NewGuid();
        var context = new CurrentUserContext
        {
            RoleScopes = new[] { new UserRoleScope("Manager", organizationId, assignedStoreId, null) }
        };

        Assert.False(ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.ProductsManage, context, organizationId, Guid.NewGuid(), null));
        Assert.True(ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.ProductsManage, context, organizationId, assignedStoreId, null));

        var effectiveScope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.ProductsManage, context);
        Assert.Empty(effectiveScope.OrganizationIds);
        Assert.Contains(assignedStoreId, effectiveScope.StoreIds);
    }

    [Fact]
    public async Task UpdateProduct_RejectsProductOwnedByAnotherOrganization()
    {
        var routeOrganizationId = Guid.NewGuid();
        var product = ProductFor(Guid.NewGuid());
        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(product.Id, false, Arg.Any<CancellationToken>()).Returns(product);

        var result = await new UpdateProductCommandHandler(
            store,
            Substitute.For<ITechnicalResourceMutationPolicy>(),
            InlineTechnicalResourceMutationCoordinator.Instance).HandleAsync(new UpdateProductCommand
            {
                Scope = new ProductManagementCommandScope(Manager(routeOrganizationId), routeOrganizationId),
                ProductId = product.Id,
                Request = new UpdateProductRequest { Name = "Cross tenant update" }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProduct_RejectsTechnicalIdentityChange_WhenPackageManaged()
    {
        var organizationId = Guid.NewGuid();
        var product = ProductFor(organizationId);
        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(product.Id, false, Arg.Any<CancellationToken>()).Returns(product);
        var ownership = Substitute.For<ITechnicalResourceMutationPolicy>();
        ownership.ValidateDefinitionMutationAsync(TechnicalResourceKind.Product, product.Id,
                Arg.Any<CancellationToken>())
            .Returns("Package-managed technical configuration must be forked before its definition can be changed.");

        var result = await new UpdateProductCommandHandler(
            store, ownership, InlineTechnicalResourceMutationCoordinator.Instance).HandleAsync(new UpdateProductCommand
            {
                Scope = new ProductManagementCommandScope(Manager(organizationId), organizationId),
                ProductId = product.Id,
                Request = new UpdateProductRequest { Code = "CHANGED" }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProduct_RejectsCurrencyChange_WhenReferencedByMenuItem()
    {
        var organizationId = Guid.NewGuid();
        var product = ProductFor(organizationId);
        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(product.Id, false, Arg.Any<CancellationToken>()).Returns(product);
        store.IsProductReferencedByMenuItemsAsync(product.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new UpdateProductCommandHandler(
            store,
            Substitute.For<ITechnicalResourceMutationPolicy>(),
            InlineTechnicalResourceMutationCoordinator.Instance).HandleAsync(new UpdateProductCommand
            {
                Scope = new ProductManagementCommandScope(Manager(organizationId), organizationId),
                ProductId = product.Id,
                Request = new UpdateProductRequest { Currency = "USD" }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("VND", product.Currency);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProduct_RejectsProductReferencedByMenuItem()
    {
        var organizationId = Guid.NewGuid();
        var product = ProductFor(organizationId);
        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(product.Id, false, Arg.Any<CancellationToken>()).Returns(product);
        store.IsProductReferencedByMenuItemsAsync(product.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new DeleteProductCommandHandler(
            store,
            Substitute.For<ITechnicalResourceMutationPolicy>(),
            InlineTechnicalResourceMutationCoordinator.Instance).HandleAsync(new DeleteProductCommand
            {
                Scope = new ProductManagementCommandScope(Manager(organizationId), organizationId),
                ProductId = product.Id
            });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Null(product.DeletedAt);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProductVariant_RejectsVariantReferencedByMenuItem()
    {
        var organizationId = Guid.NewGuid();
        var product = ProductFor(organizationId);
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Code = "DEFAULT",
            Name = "Default"
        };
        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(product.Id, true, Arg.Any<CancellationToken>()).Returns(product);
        store.GetProductVariantByIdAsync(product.Id, variant.Id, false, Arg.Any<CancellationToken>())
            .Returns(variant);
        store.IsProductVariantReferencedByMenuItemsAsync(variant.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await new DeleteProductVariantCommandHandler(
            store,
            Substitute.For<ITechnicalResourceMutationPolicy>(),
            InlineTechnicalResourceMutationCoordinator.Instance).HandleAsync(new DeleteProductVariantCommand
            {
                Scope = new ProductManagementCommandScope(Manager(organizationId), organizationId),
                ProductId = product.Id,
                VariantId = variant.Id
            });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Null(variant.DeletedAt);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_RejectsStoreOutsideRouteOrganization()
    {
        var organizationId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var store = Substitute.For<IProductStore>();
        store.TenantScopeExistsAsync(organizationId, storeId, null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await new CreateProductCommandHandler(store).HandleAsync(new CreateProductCommand
        {
            Scope = new ProductManagementCommandScope(Manager(organizationId), organizationId),
            Request = new CreateProductRequest
            {
                StoreId = storeId,
                Code = "COFFEE",
                Name = "Coffee",
                BasePrice = 10_000
            }
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Product scope does not belong to the route organization.", result.Message);
        await store.DidNotReceive().AddProductAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMenu_RejectsMenuOwnedByAnotherOrganization()
    {
        var routeOrganizationId = Guid.NewGuid();
        var menu = new Menu
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            ScopeType = TenantScopeType.Organization,
            Code = "MAIN",
            Name = "Main",
            Currency = "VND"
        };
        var store = Substitute.For<IMenuStore>();
        store.GetMenuByIdAsync(menu.Id, false, Arg.Any<CancellationToken>()).Returns(menu);

        var result = await new UpdateMenuCommandHandler(
            store, InlineTechnicalResourceMutationCoordinator.Instance).HandleAsync(new UpdateMenuCommand
            {
                Scope = new MenuManagementCommandScope(Manager(routeOrganizationId), routeOrganizationId),
                MenuId = menu.Id,
                Request = new UpdateMenuRequest { Name = "Cross tenant update" }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrganizationRoute_DoesNotMutateGlobalProductTemplate()
    {
        var organizationId = Guid.NewGuid();
        var template = ProductFor(null, TenantScopeType.Global);
        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(template.Id, false, Arg.Any<CancellationToken>()).Returns(template);

        var result = await new SetProductAvailabilityCommandHandler(store).HandleAsync(
            new SetProductAvailabilityCommand
            {
                Scope = new ProductManagementCommandScope(Manager(organizationId), organizationId),
                ProductId = template.Id,
                IsAvailable = false
            });

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloneProductTemplate_CreatesOrganizationOwnedCopyWithLineage()
    {
        var organizationId = Guid.NewGuid();
        var template = ProductFor(null, TenantScopeType.Global);
        template.ProductVariants.Add(new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = template.Id,
            Code = "DEFAULT",
            Name = "Default",
            Currency = "VND"
        });
        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(template.Id, true, Arg.Any<CancellationToken>()).Returns(template);
        store.TenantScopeExistsAsync(organizationId, null, null, Arg.Any<CancellationToken>()).Returns(true);
        Product? saved = null;
        store.When(x => x.AddProductAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<Product>());

        var result = await new CloneProductTemplateCommandHandler(store).HandleAsync(
            new CloneProductTemplateCommand
            {
                Scope = new ProductManagementCommandScope(Manager(organizationId), organizationId),
                Request = new CloneProductTemplateRequest { TemplateProductId = template.Id }
            });

        Assert.True(result.Succeeded, result.Message);
        Assert.NotNull(saved);
        Assert.Equal(organizationId, saved.OrganizationId);
        Assert.Equal(template.Id, saved.TemplateProductId);
        Assert.Equal(TenantScopeType.Organization, saved.ScopeType);
        Assert.Single(saved.ProductVariants);
    }

    [Fact]
    public async Task Manager_CanReadButCannotMutateGlobalProductTemplate()
    {
        var organizationId = Guid.NewGuid();
        var template = ProductFor(null, TenantScopeType.Global);
        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(template.Id, true, Arg.Any<CancellationToken>()).Returns(template);
        var manager = Manager(organizationId);

        var read = await new GetProductQueryHandler(store).HandleAsync(new GetProductQuery(template.Id)
        {
            UserContext = manager,
            IsGlobalTemplate = true
        });
        var mutation = await new SetProductAvailabilityCommandHandler(store).HandleAsync(
            new SetProductAvailabilityCommand
            {
                Scope = new ProductManagementCommandScope(manager, null, IsGlobalTemplate: true),
                ProductId = template.Id,
                IsAvailable = false
            });

        Assert.True(read.Succeeded, read.Message);
        Assert.False(mutation.Succeeded);
        Assert.Equal(404, mutation.StatusCode);
    }

    [Fact]
    public async Task OrgAdmin_CanReadButCannotMutateGlobalProductTemplate()
    {
        var organizationId = Guid.NewGuid();
        var template = ProductFor(null, TenantScopeType.Global);
        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(template.Id, true, Arg.Any<CancellationToken>()).Returns(template);
        var orgAdmin = OrgAdmin(organizationId);

        var read = await new GetProductQueryHandler(store).HandleAsync(new GetProductQuery(template.Id)
        {
            UserContext = orgAdmin,
            IsGlobalTemplate = true
        });
        var mutation = await new SetProductAvailabilityCommandHandler(store).HandleAsync(
            new SetProductAvailabilityCommand
            {
                Scope = new ProductManagementCommandScope(orgAdmin, null, IsGlobalTemplate: true),
                ProductId = template.Id,
                IsAvailable = false
            });

        Assert.True(read.Succeeded, read.Message);
        Assert.False(mutation.Succeeded);
        Assert.Equal(404, mutation.StatusCode);
    }

    [Fact]
    public async Task ProductReadModel_ReportsRecipeReadinessPerVariant()
    {
        var organizationId = Guid.NewGuid();
        var product = ProductFor(organizationId);
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Code = "MACHINE",
            Name = "Machine produced",
            Currency = "VND"
        };
        variant.Recipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            Code = "DRAFT",
            Name = "Draft recipe",
            Status = Domain.Catalog.Enums.RecipeStatus.Draft
        });
        variant.Recipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            Code = "PUBLISHED",
            Name = "Published recipe",
            Status = Domain.Catalog.Enums.RecipeStatus.Published
        });
        product.ProductVariants.Add(variant);

        var store = Substitute.For<IProductStore>();
        store.GetProductByIdAsync(product.Id, true, Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await new GetProductQueryHandler(store).HandleAsync(new GetProductQuery(product.Id)
        {
            UserContext = OrgAdmin(organizationId),
            OrganizationId = organizationId
        });

        Assert.True(result.Succeeded, result.Message);
        var mappedVariant = Assert.Single(result.Data!.Variants);
        Assert.Equal(2, mappedVariant.RecipeCount);
        Assert.Equal(1, mappedVariant.SellableRecipeCount);
    }

    private static Product ProductFor(Guid? organizationId, TenantScopeType scopeType = TenantScopeType.Organization) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        ScopeType = scopeType,
        Code = "PRODUCT",
        Name = "Product",
        Currency = "VND"
    };

    private static CurrentUserContext Manager(Guid organizationId) => new()
    {
        AccountId = Guid.NewGuid(),
        AllowedOrganizationIds = new HashSet<Guid> { organizationId },
        RoleScopes = new[] { new UserRoleScope("Manager", organizationId, null, null) }
    };

    private static CurrentUserContext OrgAdmin(Guid organizationId) => new()
    {
        AccountId = Guid.NewGuid(),
        AllowedOrganizationIds = new HashSet<Guid> { organizationId },
        RoleScopes = new[] { new UserRoleScope("OrgAdmin", organizationId, null, null) }
    };
}
