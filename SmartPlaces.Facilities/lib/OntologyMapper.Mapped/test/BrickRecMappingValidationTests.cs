// -----------------------------------------------------------------------
// <copyright file="BrickRecMappingValidationTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.SmartPlaces.Facilities.OntologyMapper.Mapped.Test
{
    using DTDLParser;
    using Microsoft.Extensions.Logging;
    using Microsoft.SmartPlaces.Facilities.OntologyMapper;
    using Moq;
    using System.Reflection;
    using Xunit;
    using Xunit.Abstractions;

    public class BrickRecMappingValidationTests
    {
        private readonly ITestOutputHelper output;

        public BrickRecMappingValidationTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("Mappings.v0.BrickRec.mapped_json_v0_dtdlv2_Brick_1_3-REC_4_0.json")]
        [InlineData("Mappings.v0.BrickRec.mapped_json_v0_dtdlv3_Brick_1_3-REC_4_0.json")]
        [InlineData("Mappings.v1.BrickRec.mapped_v1_dtdlv2_Brick_1_3-REC_4_0.json")]
        public void ValidateEmbeddedResourceDtmisAreValidFormat(string resourcePath)
        {
            var mockLogger = new Mock<ILogger>();
            var resourceLoader = new MappedOntologyMappingLoader(mockLogger.Object, resourcePath);
            var ontologyMappingManager = new OntologyMappingManager(resourceLoader);

            var exceptions = new List<string>();
            foreach (var mapping in ontologyMappingManager.OntologyMapping.InterfaceRemaps)
            {
                try
                {
                    var inputDtmi = new Dtmi(mapping.InputDtmi);
                }
                catch (ParsingException)
                {
                    exceptions.Add($"Invalid input DTMI: {mapping.InputDtmi}");
                }

                try
                {
                    var outputDtmi = new Dtmi(mapping.OutputDtmi);
                }
                catch (ParsingException)
                {
                    exceptions.Add($"Invalid output DTMI: {mapping.OutputDtmi}");
                }
            }

            // Verify that the Interface Remaps are unique for an input interface
            foreach (var interfaceRemap in ontologyMappingManager.OntologyMapping.InterfaceRemaps)
            {
                var matchingRemapsCount = ontologyMappingManager.OntologyMapping.InterfaceRemaps.Count(p => p.InputDtmi == interfaceRemap.InputDtmi);
                if (matchingRemapsCount > 1)
                {
                    exceptions.Add($"Duplicate InterfaceRemap: {interfaceRemap.InputDtmi}");
                }
            }

            // Verify that the Interface Remaps are unique for an input interface
            foreach (var relationshipRemap in ontologyMappingManager.OntologyMapping.RelationshipRemaps)
            {
                var matchingRemapsCount = ontologyMappingManager.OntologyMapping.RelationshipRemaps.Count(p => p.InputRelationship == relationshipRemap.InputRelationship);
                if (matchingRemapsCount > 1)
                {
                    exceptions.Add($"Duplicate RelationshipRemap: {relationshipRemap.InputRelationship}");
                }
            }

            // Verify that the property projections are unique for an output property
            foreach (var projection in ontologyMappingManager.OntologyMapping.PropertyProjections)
            {
                var matchingProjectionsCount = ontologyMappingManager.OntologyMapping.PropertyProjections.Count(p => p.OutputPropertyName == projection.OutputPropertyName);
                if (matchingProjectionsCount > 1)
                {
                    exceptions.Add($"Duplicate PropertyProjection: {projection.OutputPropertyName}");
                }
            }

            // Verify that the fill properties are unique for an output property
            foreach (var fillProperty in ontologyMappingManager.OntologyMapping.FillProperties)
            {
                var matchingFillPropertyCount = ontologyMappingManager.OntologyMapping.FillProperties.Count(p => p.OutputPropertyName == fillProperty.OutputPropertyName);
                if (matchingFillPropertyCount > 1)
                {
                    exceptions.Add($"Duplicate FillProperty: {fillProperty.OutputPropertyName}");
                }
            }

            Assert.Empty(exceptions);
        }

        [Theory]
        [InlineData("Mappings.v1.BrickRec.mapped_v1_dtdlv2_Brick_1_3-REC_4_0.json")]
        public void ValidateSourceDtmisAreValid(string resourcePath)
        {
            var mockLogger = new Mock<ILogger>();
            var resourceLoader = new MappedOntologyMappingLoader(mockLogger.Object, resourcePath);
            var ontologyMappingManager = new OntologyMappingManager(resourceLoader);
            var modelParser = new ModelParser();
            var inputDtmi = LoadDtdl("mapped_dtdl.json");
            var inputModels = modelParser.Parse(inputDtmi);
            ontologyMappingManager.ValidateSourceOntologyMapping(inputModels, out var invalidSources);

            Assert.Empty(invalidSources);
        }

        [Theory]
        [InlineData("Mappings.v1.BrickRec.mapped_v1_dtdlv2_Brick_1_3-REC_4_0.json")]
        public void ValidateTargetDtmisAreValid(string resourcePath)
        {
            var mockLogger = new Mock<ILogger>();
            var resourceLoader = new MappedOntologyMappingLoader(mockLogger.Object, resourcePath);
            var ontologyMappingManager = new OntologyMappingManager(resourceLoader);
            var modelParser = new ModelParser();
            var inputDtmi = LoadDtdl("RealEstateCore.DTDLv2.jsonld");
            var inputModels = modelParser.Parse(inputDtmi);
            ontologyMappingManager.ValidateTargetOntologyMapping(inputModels, out var invalidSources);

            Assert.Empty(invalidSources);
        }

        private IEnumerable<string> LoadDtdl(string dtdlFile)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(dtdlFile));
            List<string> dtdls = new List<string>();

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string result = reader.ReadToEnd();
                        dtdls.Add(result);
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceName);
                }
            }

            return dtdls;
        }
    }
}