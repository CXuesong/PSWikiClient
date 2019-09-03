using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PSWikiClient.Infrastructures;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;

namespace PSWikiClient.Wikibase
{

    [Cmdlet(VerbsCommon.New, NounsWikibase.Entity)]
    [OutputType(typeof(Entity))]
    public class NewEntityCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        [Parameter(ParameterSetName = "NewItem", Mandatory = true)]
        public SwitchParameter Item { get; set; }

        [Parameter(ParameterSetName = "NewProperty", Mandatory = true)]
        public SwitchParameter Property { get; set; }

        [Parameter(ParameterSetName = "NewProperty", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string DataType { get; set; }

        /// <summary>Labels and aliases of the entity.</summary>
        [Parameter]
        public WbMonolingualText[] Labels { get; set; }

        [Parameter]
        public WbMonolingualText[] Descriptions { get; set; }

        [Alias("Comment")]
        public string Summary { get; set; }

        [Parameter]
        public SwitchParameter Bot { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            Entity entity;
            var changes = new List<EntityEditEntry>();
            if (Item)
            {
                entity = new Entity(WikiSite, EntityType.Item);
            }
            else if (Property)
            {
                entity = new Entity(WikiSite, EntityType.Property);
                var type = BuiltInDataTypes.Get(DataType);
                if (type == null) throw new ArgumentException("Invalid data type name.", nameof(DataType));
                changes.Add(new EntityEditEntry(nameof(entity.DataType), type));
            }
            else
            {
                throw new InvalidOperationException();
            }
            if (Labels != null)
            {
                var labeledLang = new HashSet<string>();
                foreach (var l in Labels)
                {
                    var lang = l.Language.ToLowerInvariant();
                    if (labeledLang.Contains(lang))
                    {
                        changes.Add(new EntityEditEntry(nameof(entity.Aliases), l));
                    }
                    else
                    {
                        changes.Add(new EntityEditEntry(nameof(entity.Labels), l));
                    }
                }
            }
            if (Descriptions != null)
            {
                foreach (var d in Descriptions)
                {
                    changes.Add(new EntityEditEntry(nameof(entity.Descriptions), d));
                }
            }
            await entity.EditAsync(changes, Summary,
                WikibaseUtility.MakeEntityEditOptions(bulk: true, bot: Bot), cancellationToken);
            WriteObject(entity);
        }
    }

    [Cmdlet(VerbsCommon.Get, NounsWikibase.Entity)]
    [OutputType(typeof(Entity))]
    public class GetEntityCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Ids")]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "Entities")]
        [ValidateNotNullOrEmpty]
        public Entity[] Entity { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ParameterSetName = "Ids")]
        [ValidateNotNullOrEmpty]
        public string[] Id { get; set; }

        [Parameter]
        public string[] Languages { get; set; }

        [Parameter]
        public SwitchParameter Descriptions { get; set; }

        [Parameter]
        public SwitchParameter Claims { get; set; }

        [Parameter]
        public SwitchParameter SiteLinks { get; set; }

        private Entity[] GetEntities()
        {
            if (Entity != null) return Entity;
            if (Id != null) return Id.Select(t => new Entity(WikiSite, t)).ToArray();
            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var entities = GetEntities();
            var options = EntityQueryOptions.FetchInfo
                          | EntityQueryOptions.FetchLabels
                          | EntityQueryOptions.FetchAliases;
            if (Descriptions) options |= EntityQueryOptions.FetchDescriptions;
            if (Claims) options |= EntityQueryOptions.FetchClaims;
            if (SiteLinks) options |= EntityQueryOptions.FetchSiteLinks;
            var languages = Languages ?? WikibaseUtility.GetLocalLanguages();
            if (languages.Count == 1 && languages[0] == "*") languages = null;
            await entities.RefreshAsync(options, languages, cancellationToken);
            WriteObject(entities, true);
        }
    }

    [Cmdlet(VerbsCommon.Set, NounsWikibase.Label, SupportsShouldProcess = true)]
    public class SetLabelCommand : AsyncCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ById")]
        [ValidateNotNullOrEmpty]
        public string Id { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEntity")]
        [ValidateNotNull]
        public Entity Entity { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ById")]
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ByEntity")]
        public WbMonolingualText[] Labels { get; set; }

        [Parameter]
        [Alias("Comment")]
        public string Summary { get; set; }

        [Parameter]
        public SwitchParameter Bot { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            if (Labels == null || Labels.Length == 0) return;
            var entity = Entity ?? new Entity(WikiSite, Id);
            if (!ShouldProcess(string.Format("Setting {0} labels for {1}.", Labels.Length, entity.Id))) return;
            await entity.EditAsync(Labels.Select(l => new EntityEditEntry(nameof(entity.Labels), l)),
                Summary, WikibaseUtility.MakeEntityEditOptions(bot: Bot), cancellationToken);
        }
    }

    [Cmdlet(VerbsCommon.Add, NounsWikibase.Claim, SupportsShouldProcess = true)]
    public class AddClaimCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ParameterSetName = "ById")]
        [ValidateNotNullOrEmpty]
        public string Id { get; set; }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "ByEntity")]
        [ValidateNotNull]
        public Entity Entity { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ById")]
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ByEntity")]
        public string Property { get; set; }

        [Parameter(Mandatory = true, Position = 3, ParameterSetName = "ById")]
        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ByEntity")]
        [ValidateNotNull]
        public object Value { get; set; }

        [Parameter]
        [Alias("Comment")]
        public string Summary { get; set; }

        [Parameter]
        public SwitchParameter Bot { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var entity = Entity ?? new Entity(WikiSite, Id);
            var summary = Summary;
            var property = new Entity(entity.Site, Property);
            await property.RefreshAsync(EntityQueryOptions.FetchInfo, null, cancellationToken);
            if (!ShouldProcess(string.Format("{0}.{1} := {2}", entity.Id, property.Id, Value))) return;
            await entity.EditAsync(new[]
            {
                new EntityEditEntry(nameof(entity.Claims), new Claim(property.Id)
                {
                    MainSnak =
                    {
                        DataType = property.DataType,
                        DataValue = Value,
                    }
                })
            }, summary, WikibaseUtility.MakeEntityEditOptions(bot: Bot), cancellationToken);
        }
    }

    [Cmdlet(VerbsCommon.Copy, NounsWikibase.Entity, SupportsShouldProcess = true)]
    [OutputType(typeof(Entity))]
    public class CopyEntityCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite SourceSite { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string SourceId { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        [ValidateNotNull]
        public WikiSite DestinationSite { get; set; }

        [Parameter]
        public SwitchParameter Bot { get; set; }

        [Parameter]
        public string[] Languages { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var options = EntityEditOptions.None;
            if (Bot) options |= EntityEditOptions.Bot;
            var entity = new Entity(SourceSite, SourceId);
            await entity.RefreshAsync(EntityQueryOptions.FetchLabels
                                      | EntityQueryOptions.FetchAliases
                                      | EntityQueryOptions.FetchDescriptions
                                      | EntityQueryOptions.FetchInfo, Languages, cancellationToken);
            if (!entity.Exists) throw new InvalidOperationException($"The source entity {entity} does not exist.");
            var dest = new Entity(DestinationSite, entity.Type);
            var changes = new List<EntityEditEntry>();
            changes.AddRange(entity.Labels.Select(l => new EntityEditEntry(nameof(dest.Labels), l)));
            changes.AddRange(entity.Descriptions.Select(d => new EntityEditEntry(nameof(dest.Descriptions), d)));
            changes.AddRange(entity.Aliases.Select(a => new EntityEditEntry(nameof(dest.Aliases), a)));
            if (entity.Type == EntityType.Property)
                changes.Add(new EntityEditEntry(nameof(dest.DataType), entity.DataType));
            if (!ShouldProcess(string.Format("Copy {0} from {1} to {2} with {3} changes.", entity, entity.Site, dest.Site, changes.Count)))
            {
                WriteObject(dest);
                return;
            }
            await dest.EditAsync(changes, $"Copied from \"{entity}\" on {SourceSite}.", options, cancellationToken);
            WriteCommandDetail($"Copied \"{entity}\"({SourceSite}) to {dest.Id}({DestinationSite})");
            WriteObject(dest);
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "ClaimMapping")]
    [OutputType(typeof(string))]
    public class TestEntityMappingCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite SourceSite { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string SourceId { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        [ValidateNotNullOrEmpty]
        public string[] Property { get; set; }

        [Parameter(Mandatory = true, Position = 3)]
        public IDictionary EntityMapping { get; set; }

        [Parameter]
        public string[] OptionalProperty { get; set; }

        private IEnumerable<string> FindMissingEntities(Snak snak)
        {
            if (EntityMapping == null) yield break;
            if (EntityMapping[snak.PropertyId] == null)
                yield return snak.PropertyId;
            if (snak.DataType == BuiltInDataTypes.WikibaseItem || snak.DataType == BuiltInDataTypes.WikibaseProperty)
            {
                var src = (string)snak.DataValue;
                if (!string.IsNullOrEmpty(src) && EntityMapping[src] == null)
                    yield return src;
            }
        }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var entity = new Entity(SourceSite, SourceId);
            await entity.RefreshAsync(EntityQueryOptions.FetchClaims
                                      | EntityQueryOptions.FetchInfo, null, cancellationToken);
            if (!entity.Exists) throw new InvalidOperationException($"The source entity {entity} does not exist.");
            var optionalProp = OptionalProperty == null ? null : new HashSet<string>(OptionalProperty);
            var missingEntities = new HashSet<string>();
            IEnumerable<string> props = Property;
            if (Property.Length == 1 && Property[0] == "*")
            {
                if (EntityMapping == null) throw new ArgumentNullException(nameof(EntityMapping));
                props = EntityMapping.Keys.Cast<string>();
            }
            foreach (var prop in props)
            {
                if (prop == null) throw new ArgumentException("Properties have null item.", nameof(Property));
                var pc = entity.Claims[prop.Trim().ToUpperInvariant()];
                foreach (var claim in pc)
                {
                    if (optionalProp == null || !optionalProp.Contains(claim.MainSnak.PropertyId))
                        missingEntities.AddRange(FindMissingEntities(claim.MainSnak));
                    foreach (var q in claim.Qualifiers)
                    {
                        if (optionalProp == null || !optionalProp.Contains(q.PropertyId))
                            missingEntities.AddRange(FindMissingEntities(q));
                    }
                }
            }
            WriteObject(missingEntities, true);
        }
    }

    [Cmdlet(VerbsCommon.Copy, NounsWikibase.Claims, SupportsShouldProcess = true)]
    public class CopyClaimsCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite SourceSite { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string SourceId { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        [ValidateNotNull]
        public WikiSite DestinationSite { get; set; }

        [Parameter(Mandatory = true, Position = 3)]
        [ValidateNotNullOrEmpty]
        public string DestinationId { get; set; }

        [Parameter(Mandatory = true, Position = 4)]
        [ValidateNotNullOrEmpty]
        public string[] Property { get; set; }

        /// <summary>
        /// <para type="description">Specifies the source property IDs whose entity reference values
        /// can be replaced with missing references.</para>
        /// </summary>
        [Parameter]
        public string[] OptionalProperty { get; set; }

        [Parameter]
        public IDictionary EntityMapping { get; set; }

        [Parameter]
        public string CitationProperty { get; set; }

        [Parameter]
        public SwitchParameter Bot { get; set; }

        [Parameter]
        public SwitchParameter Progressive { get; set; }

        private WikibaseSiteInfo sourceSiteWikibaseInfo, destinationSiteWikibaseInfo;

        private Uri MapEntityUri(Uri uri, bool optional)
        {
            if (EntityMapping == null) return uri;
            // For quantities without units
            if (uri == WbQuantity.Unity) return uri;
            var sourceId = sourceSiteWikibaseInfo.ParseEntityId(uri.ToString());
            var mapped = (string)EntityMapping[sourceId];
            if (mapped != null) return WikibaseUriFactory.Get(destinationSiteWikibaseInfo.MakeEntityUri(mapped));
            if (!optional) throw new KeyNotFoundException($"Cannot find mapped entity for {uri}.");
            return null;
        }

        private string MapEntity(string id, bool optional)
        {
            if (EntityMapping == null) return id;
            var mapped = (string)EntityMapping[id];
            if (mapped != null) return mapped;
            if (!optional) throw new KeyNotFoundException($"Cannot find mapped entity for {id}.");
            return null;
        }

        private void FixValueEntityReference(Snak snak, bool optional)
        {
            if (EntityMapping == null) return;
            if (snak.DataType == BuiltInDataTypes.WikibaseItem || snak.DataType == BuiltInDataTypes.WikibaseProperty)
            {
                var src = (string)snak.DataValue;
                if (string.IsNullOrEmpty(src)) return;
                var mapped = MapEntity(src, optional);
                if (mapped == null) snak.SnakType = SnakType.SomeValue;
                snak.DataValue = mapped;
            }
            else if (snak.DataType == BuiltInDataTypes.Quantity)
            {
                // TODO WbQuantity is not precise enough!
                var src = (WbQuantity)snak.DataValue;
                var mappedUnit = MapEntityUri(src.Unit, optional);
                if (mappedUnit == null)
                {
                    snak.SnakType = SnakType.SomeValue;
                }
                else
                {
                    snak.DataValue = new WbQuantity(src.Amount, src.LowerBound, src.UpperBound, mappedUnit);
                }
            }
        }

        // snak1: base/theirs, snak2: ours
        private bool SnakValueEquals(Snak snak1, Snak snak2, bool valueOptional)
        {
            if (snak1.SnakType != SnakType.Value) return snak2.SnakType == snak1.SnakType;
            if (snak1.DataType == BuiltInDataTypes.WikibaseItem || snak1.DataType == BuiltInDataTypes.WikibaseProperty)
            {
                var src = MapEntity((string)snak1.DataValue, valueOptional);
                var dest = (string)snak2.DataValue;
                if (string.IsNullOrEmpty(src))
                {
                    return snak2.SnakType == SnakType.SomeValue;
                }
                return src == dest;
            }
            if (snak1.DataType == BuiltInDataTypes.Quantity)
            {
                var value1 = (WbQuantity)snak1.DataValue;
                var value2 = (WbQuantity)snak2.DataValue;
                var mappedUnit = MapEntityUri(value1.Unit, true);
                if (mappedUnit == null) return false;
                var mapped1 = new WbQuantity(value1.Amount, value1.LowerBound, value1.UpperBound, mappedUnit);
                return mapped1.Equals(value2);
            }
            return JToken.DeepEquals(snak1.RawDataValue, snak2.RawDataValue);
        }

        private Snak CloneSnak(Snak snak, bool valueOptional)
        {
            var propId = MapEntity(snak.PropertyId, true);
            if (propId == null) return null;
            var newSnak = new Snak(propId)
            {
                SnakType = snak.SnakType,
                DataType = snak.DataType,
                RawDataValue = snak.RawDataValue
            };
            FixValueEntityReference(newSnak, valueOptional);
            return newSnak;
        }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var options = EntityEditOptions.Bulk;
            if (Bot) options |= EntityEditOptions.Bot;
            var entity = new Entity(SourceSite, SourceId);
            await entity.RefreshAsync(EntityQueryOptions.FetchClaims
                                      | EntityQueryOptions.FetchInfo, null, cancellationToken);
            if (!entity.Exists) throw new InvalidOperationException($"The source entity {entity} does not exist.");
            var dest = new Entity(DestinationSite, DestinationId);
            await dest.RefreshAsync(EntityQueryOptions.FetchInfo | EntityQueryOptions.FetchClaims);
            if (!dest.Exists) throw new InvalidOperationException($"The destination entity {dest} does not exist.");

            sourceSiteWikibaseInfo = WikibaseSiteInfo.FromSiteInfo(SourceSite.SiteInfo);
            destinationSiteWikibaseInfo = WikibaseSiteInfo.FromSiteInfo(DestinationSite.SiteInfo);

            var refProp = CitationProperty == null ? null : new Entity(DestinationSite, CitationProperty);
            // Lookup for the claims that are previously imported from wikidata
            // Claim ID --> Claim
            Dictionary<string, Claim> existingClaims = null;
            if (refProp != null)
            {
                await refProp.RefreshAsync(EntityQueryOptions.FetchInfo);
                existingClaims = dest.Claims.Select(c => new
                {
                    Id = c.References.Select(r =>
                            (string)r.Snaks.FirstOrDefault(s => s.PropertyId == refProp.Id)?.DataValue)
                            .FirstOrDefault(s => s != null),
                    Claim = c
                }).Where(t => t.Id != null)
                    .ToDictionary(t => t.Id, t => t.Claim);
            }
            var newClaims = new List<Claim>();
            var optionalProp = OptionalProperty == null ? null : new HashSet<string>(OptionalProperty);

            bool IsPropertyValueOptional(string propertyId)
            {
                return optionalProp?.Contains(propertyId) ?? false;
            }

            IEnumerable<string> props = Property;
            var newClaimsCounter = 0;
            var updatedClaimsCounter = 0;
            if (Property.Length == 1 && Property[0] == "*")
            {
                if (EntityMapping == null) throw new ArgumentNullException(nameof(EntityMapping));
                props = EntityMapping.Keys.Cast<string>();
            }
            foreach (var prop in props)
            {
                if (prop == null) throw new ArgumentException("Properties have null item.", nameof(Property));
                var pc = entity.Claims[prop.Trim().ToUpperInvariant()];
                foreach (var claim in pc)
                {
                    if (existingClaims != null && existingClaims.TryGetValue(claim.Id, out var newClaim))
                    {
                        var updated = false;
                        if (!SnakValueEquals(claim.MainSnak, newClaim.MainSnak, IsPropertyValueOptional(claim.MainSnak.PropertyId)))
                        {
                            newClaim.MainSnak.SnakType = claim.MainSnak.SnakType;
                            newClaim.MainSnak.RawDataValue = claim.MainSnak.RawDataValue;
                            FixValueEntityReference(newClaim.MainSnak, IsPropertyValueOptional(claim.MainSnak.PropertyId));
                            updated = true;
                        }
                        var qualifiers = new List<Snak>();
                        var reusedQs = 0;
                        var newQs = 0;
                        foreach (var q in claim.Qualifiers)
                        {
                            var mapped = MapEntity(q.PropertyId, true);
                            if (mapped == null) continue;
                            var newQ = newClaim.Qualifiers.FirstOrDefault(ourQ =>
                                ourQ.PropertyId == mapped && SnakValueEquals(q, ourQ, IsPropertyValueOptional(q.PropertyId)));
                            if (newQ == null)
                            {
                                newQs++;
                                newQ = new Snak(mapped) { DataType = q.DataType, RawDataValue = q.RawDataValue };
                                FixValueEntityReference(newQ, optionalProp?.Contains(q.PropertyId) ?? false);
                            }
                            else
                            {
                                reusedQs++;
                            }
                            qualifiers.Add(newQ);
                        }
                        if (newQs > 0 || reusedQs < newClaim.Qualifiers.Count)
                        {
                            newClaim.Qualifiers.Clear();
                            newClaim.Qualifiers.AddRange(qualifiers);
                            updated = true;
                        }
                        if (!updated) continue;
                        updatedClaimsCounter++;
                    }
                    else
                    {
                        newClaimsCounter++;
                        newClaim = new Claim(CloneSnak(claim.MainSnak, IsPropertyValueOptional(claim.MainSnak.PropertyId)));
                        foreach (var qualifier in claim.Qualifiers)
                        {
                            var snak = CloneSnak(qualifier, IsPropertyValueOptional(qualifier.PropertyId));
                            if (snak == null) continue;
                            newClaim.Qualifiers.Add(snak);
                        }
                    }
                    if (refProp != null)
                    {
                        if (newClaim.References.All(r => (string)r.Snaks.FirstOrDefault(s => s.PropertyId == refProp.Id)?.DataValue != claim.Id))
                        {
                            var refSnak = new Snak(refProp.Id, claim.Id, refProp.DataType);
                            newClaim.References.Add(new ClaimReference(refSnak));
                        }
                    }
                    newClaims.Add(newClaim);
                }
            }
            var changes = new List<EntityEditEntry>();
            changes.AddRange(newClaims.Select(c => new EntityEditEntry(nameof(dest.Claims), c)));
            if (changes.Count == 0)
            {
                WriteCommandDetail($"No matching claims to copy from \"{entity.Id}\"({SourceSite}) to {dest.Id}({DestinationSite})");
                return;
            }
            if (!ShouldProcess(string.Format("{0}({1}) --> {2}({3}), {4} changes. Claims: +{5}, !{6}",
                entity.Id, entity.Site, dest.Id, dest.Site, changes.Count, newClaimsCounter, updatedClaimsCounter)))
            {
                return;
            }
            if (Progressive)
            {
                int counter = 1;
                foreach (var change in changes)
                {
                    var claim = (Claim)change.Value;
                    if (ShouldProcess(string.Format("{0}({1}) --> {2}({3}), Target claim: {4}",
                        entity.Id, entity.Site, dest.Id, dest.Site, claim)))
                    {
                        await dest.EditAsync(new[] { change },
                            $"[{counter}/{changes.Count}] Adding {newClaimsCounter} / updating {updatedClaimsCounter} claims from \"{entity.Id}\" on {SourceSite}.",
                            options, cancellationToken);
                    }
                }
            }
            else
            {
                await dest.EditAsync(changes,
                    $"Added {newClaimsCounter} / updated {updatedClaimsCounter} claims from \"{entity.Id}\" on {SourceSite}.",
                    options, cancellationToken);
            }
            WriteCommandDetail($"Added {newClaimsCounter} / updated {updatedClaimsCounter} claims from \"{entity.Id}\"({SourceSite}) to {dest.Id}({DestinationSite})");
        }
    }

}
