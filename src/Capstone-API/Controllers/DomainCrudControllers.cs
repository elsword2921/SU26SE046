using BLL.Services.Interfaces.Common;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[Route("api/warehouses")]
public class WarehouseController(ICrudService<Warehouse> service) : CrudControllerBase<Warehouse>(service);

[Route("api/categories")]
public class CategoryController(ICrudService<Category> service) : CrudControllerBase<Category>(service);

[Route("api/vouchers")]
public class VoucherController(ICrudService<Voucher> service) : CrudControllerBase<Voucher>(service);

[Route("api/profiles")]
public class ProfileController(ICrudService<Profile> service) : CrudControllerBase<Profile>(service);

[Route("api/profile-details")]
public class ProfileDetailController(ICrudService<ProfileDetail> service) : CrudControllerBase<ProfileDetail>(service);

[Route("api/carts")]
public class CartController(ICrudService<Cart> service) : CrudControllerBase<Cart>(service);

[Route("api/cart-items")]
public class CartItemController(ICrudService<CartItem> service) : CrudControllerBase<CartItem>(service);

[Route("api/pickup-assignments")]
[Authorize(Roles = "Manager,ReceivingStaff")]
public class PickupAssignmentController(ICrudService<PickupAssignment> service) : CrudControllerBase<PickupAssignment>(service);

[Route("api/intake-batches")]
[Authorize(Roles = "Manager,ReceivingStaff,ClassificationStaff,WarehouseStaff")]
public class IntakeBatchController(ICrudService<IntakeBatch> service) : CrudControllerBase<IntakeBatch>(service);

[Route("api/shifts")]
[Authorize(Roles = "Manager")]
public class ShiftController(ICrudService<Shift> service) : CrudControllerBase<Shift>(service);

[Route("api/operational-teams")]
[Authorize(Roles = "Manager")]
public class OperationalTeamController(ICrudService<OperationalTeam> service) : CrudControllerBase<OperationalTeam>(service);

[Route("api/team-members")]
[Authorize(Roles = "Manager")]
public class TeamMemberController(ICrudService<TeamMember> service) : CrudControllerBase<TeamMember>(service);

[Route("api/classification-criteria")]
[Authorize(Roles = "Manager,ClassificationStaff")]
public class ClassificationCriteriaController(ICrudService<ClassificationCriteria> service) : CrudControllerBase<ClassificationCriteria>(service);

[Route("api/classification-criteria-options")]
[Authorize(Roles = "Manager,ClassificationStaff")]
public class ClassificationCriteriaOptionController(ICrudService<ClassificationCriteriaOption> service) : CrudControllerBase<ClassificationCriteriaOption>(service);

[Route("api/condition-questions")]
[Authorize(Roles = "Manager,ClassificationStaff")]
public class ConditionQuestionController(ICrudService<ConditionQuestion> service) : CrudControllerBase<ConditionQuestion>(service);

[Route("api/condition-answers")]
[Authorize(Roles = "Manager,ClassificationStaff")]
public class ConditionAnswerController(ICrudService<ConditionAnswer> service) : CrudControllerBase<ConditionAnswer>(service);

[Route("api/classified-items")]
[Authorize(Roles = "Manager,ClassificationStaff,WarehouseStaff")]
public class ClassifiedItemController(ICrudService<ClassifiedItem> service) : CrudControllerBase<ClassifiedItem>(service);

[Route("api/classified-batches")]
[Authorize(Roles = "Manager,ClassificationStaff,WarehouseStaff")]
public class ClassifiedBatchController(ICrudService<ClassifiedBatch> service) : CrudControllerBase<ClassifiedBatch>(service);

[Route("api/classification-results")]
[Authorize(Roles = "Manager,ClassificationStaff")]
public class ClassificationResultController(ICrudService<ClassificationResult> service) : CrudControllerBase<ClassificationResult>(service);

[Route("api/inspection-answers")]
[Authorize(Roles = "Manager,ClassificationStaff")]
public class InspectionAnswerController(ICrudService<InspectionAnswer> service) : CrudControllerBase<InspectionAnswer>(service);

[Route("api/warehouse-areas")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class WarehouseAreaController(ICrudService<WarehouseArea> service) : CrudControllerBase<WarehouseArea>(service);

[Route("api/area-groups")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class AreaGroupController(ICrudService<AreaGroup> service) : CrudControllerBase<AreaGroup>(service);

[Route("api/inventories")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class InventoryController(ICrudService<Inventory> service) : CrudControllerBase<Inventory>(service);

[Route("api/inventory-transactions")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class InventoryTransactionController(ICrudService<InventoryTransaction> service) : CrudControllerBase<InventoryTransaction>(service);

[Route("api/transaction-items")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class TransactionItemController(ICrudService<TransactionItem> service) : CrudControllerBase<TransactionItem>(service);

[Route("api/transfer-requests")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class TransferRequestController(ICrudService<TransferRequest> service) : CrudControllerBase<TransferRequest>(service);

[Route("api/transfer-items")]
[Authorize(Roles = "Manager,WarehouseStaff")]
public class TransferItemController(ICrudService<TransferItem> service) : CrudControllerBase<TransferItem>(service);

[Route("api/distribution-requests")]
[Authorize(Roles = "Manager,WarehouseStaff,CharityOrganization,RecyclingOrganization")]
public class DistributionRequestController(ICrudService<DistributionRequest> service) : CrudControllerBase<DistributionRequest>(service);

[Route("api/distribution-items")]
[Authorize(Roles = "Manager,WarehouseStaff,CharityOrganization,RecyclingOrganization")]
public class DistributionItemController(ICrudService<DistributionItem> service) : CrudControllerBase<DistributionItem>(service);
