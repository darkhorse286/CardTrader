using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

namespace CardTrader.Integration.Tests.Infrastructure;

// Programmatic mirror of authz/model.fga — kept in sync with the copy in
// CardTrader.Authorization.Tests.AuthorizationModelBuilder.
internal static class AuthorizationModelBuilder
{
    private const string NotExpired = "not_expired";

    public static ClientWriteAuthorizationModelRequest Build() => new()
    {
        SchemaVersion = "1.1",
        TypeDefinitions =
        [
            UserType(),
            CardInstanceType(),
            CollectionType(),
            TradeProposalType(),
            DelegationType(),
        ],
        Conditions = new Dictionary<string, Condition>
        {
            [NotExpired] = new()
            {
                Name = NotExpired,
                Expression = "current_time < expires_at",
                Parameters = new Dictionary<string, ConditionParamTypeRef>
                {
                    ["current_time"] = new() { TypeName = TypeName.TYPENAMETIMESTAMP },
                    ["expires_at"]   = new() { TypeName = TypeName.TYPENAMETIMESTAMP },
                },
            },
        },
    };

    private static TypeDefinition UserType() => new()
    {
        Type = FgaTypes.User,
        Relations = new Dictionary<string, Userset>(),
    };

    private static TypeDefinition CardInstanceType() => new()
    {
        Type = FgaTypes.CardInstance,
        Relations = new Dictionary<string, Userset>
        {
            [FgaRelations.Owner]      = Direct(),
            [FgaRelations.Viewer]     = Direct(),
            [FgaRelations.Collection] = Direct(),
            [FgaRelations.CanManage]  = Computed(FgaRelations.Owner),
            [FgaRelations.CanTrade]   = Computed(FgaRelations.Owner),
            [FgaRelations.CanView]    = Union(
                Computed(FgaRelations.Owner),
                Computed(FgaRelations.Viewer),
                TupleToUs(FgaRelations.Collection, FgaRelations.CanView)),
        },
        Metadata = new Metadata
        {
            Relations = new Dictionary<string, RelationMetadata>
            {
                [FgaRelations.Owner]      = Types(User()),
                [FgaRelations.Viewer]     = Types(User()),
                [FgaRelations.Collection] = Types(TypeRef(FgaTypes.Collection)),
            },
        },
    };

    private static TypeDefinition CollectionType() => new()
    {
        Type = FgaTypes.Collection,
        Relations = new Dictionary<string, Userset>
        {
            [FgaRelations.Owner]     = Direct(),
            [FgaRelations.Viewer]    = Direct(),
            [FgaRelations.CanManage] = Computed(FgaRelations.Owner),
            [FgaRelations.CanView]   = Union(
                Computed(FgaRelations.Owner),
                Computed(FgaRelations.Viewer)),
        },
        Metadata = new Metadata
        {
            Relations = new Dictionary<string, RelationMetadata>
            {
                [FgaRelations.Owner]  = Types(User()),
                [FgaRelations.Viewer] = Types(User(), Wildcard()),
            },
        },
    };

    private static TypeDefinition TradeProposalType() => new()
    {
        Type = FgaTypes.TradeProposal,
        Relations = new Dictionary<string, Userset>
        {
            [FgaRelations.Initiator]   = Direct(),
            [FgaRelations.Recipient]   = Direct(),
            [FgaRelations.Supervisor]  = Direct(),
            [FgaRelations.Facilitator] = Direct(),
            [FgaRelations.CanAccept]   = Computed(FgaRelations.Recipient),
            [FgaRelations.CanCancel]   = Union(
                Computed(FgaRelations.Initiator),
                Computed(FgaRelations.Supervisor)),
            [FgaRelations.CanView]     = Union(
                Computed(FgaRelations.Initiator),
                Computed(FgaRelations.Recipient),
                Computed(FgaRelations.Supervisor),
                Computed(FgaRelations.Facilitator)),
        },
        Metadata = new Metadata
        {
            Relations = new Dictionary<string, RelationMetadata>
            {
                [FgaRelations.Initiator]   = Types(User()),
                [FgaRelations.Recipient]   = Types(User()),
                [FgaRelations.Supervisor]  = Types(TypeRelation(FgaTypes.Delegation, FgaRelations.ActiveDelegator)),
                [FgaRelations.Facilitator] = Types(User()),
            },
        },
    };

    private static TypeDefinition DelegationType() => new()
    {
        Type = FgaTypes.Delegation,
        Relations = new Dictionary<string, Userset>
        {
            [FgaRelations.Delegator]       = Direct(),
            [FgaRelations.Delegatee]       = Direct(),
            [FgaRelations.ActiveDelegator] = Direct(),
        },
        Metadata = new Metadata
        {
            Relations = new Dictionary<string, RelationMetadata>
            {
                [FgaRelations.Delegator]       = Types(User()),
                [FgaRelations.Delegatee]       = Types(User()),
                [FgaRelations.ActiveDelegator] = Types(User(), UserWithCondition(NotExpired)),
            },
        },
    };

    private static Userset Direct() => new() { This = new object() };
    private static Userset Computed(string relation) => new()
        { ComputedUserset = new ObjectRelation { Relation = relation } };
    private static Userset TupleToUs(string tupleset, string relation) => new()
    {
        TupleToUserset = new TupleToUserset
        {
            Tupleset = new ObjectRelation { Relation = tupleset },
            ComputedUserset = new ObjectRelation { Relation = relation },
        },
    };
    private static Userset Union(params Userset[] children) => new()
        { Union = new Usersets { Child = [..children] } };

    private static RelationMetadata Types(params RelationReference[] refs) => new()
        { DirectlyRelatedUserTypes = [..refs] };
    private static RelationReference User() => new() { Type = FgaTypes.User };
    private static RelationReference Wildcard() => new()
        { Type = FgaTypes.User, Wildcard = new TypedWildcard { Type = FgaTypes.User } };
    private static RelationReference UserWithCondition(string condition) => new()
        { Type = FgaTypes.User, Condition = condition };
    private static RelationReference TypeRef(string type) => new() { Type = type };
    private static RelationReference TypeRelation(string type, string relation) => new()
        { Type = type, Relation = relation };
}
