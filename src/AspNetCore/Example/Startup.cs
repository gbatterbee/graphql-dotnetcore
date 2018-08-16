using GraphQL;
using GraphQL.Http;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Example
{
    public static class JsonData
    {
        public const string tasks =
            @"{
                ""id"": 1,
                ""itemTypes"": [{ ""id"": ""otherInformation"", ""variantId"": ""uk"", ""order"": 1 }]
            }";

        public const string itemTypes =
            @"{
                ""otherInformation"": {
                ""description"": ""Other Information"",
                ""baseType"": ""ItemMemo"", 
                }
            }";

        public static dynamic productVersionData =
            @"{
                ""otherInformation"": {
                    ""value"": ""it's a little fish that can be found in Hawaii"",
                },
            }";
    }

    public class JsonApis
    {
        public JObject GetTask() => JObject.Parse(JsonData.tasks);
        public JObject GetItemTypes() => JObject.Parse(JsonData.itemTypes);
        public JObject GetProductVersionData() => JObject.Parse(JsonData.productVersionData);
    }

    public class Query
    {
        [GraphQLMetadata("task")]
        public dynamic GetTask()
        {

            var apis = new JsonApis();
            var id = apis.GetTask()["id"];
            return new
            {
                id = apis.GetTask()["id"],
                itemTypes =
                    apis.GetItemTypes().Properties().Select(p =>
                    new
                    {
                        itemTypeId = p.Name,
                        description = p.Value["description"],
                        baseType = p.Value["baseType"],
                        value = new
                        {
                            value = apis.GetProductVersionData()[p.Name]["value"]
                        }
                    }).ToArray()
            };
        }
    }

    public class Mutation
    {
        [GraphQLMetadata("task")]
        public dynamic submitData(dynamic data)
        {
            return new
            {
                Id = "1",
                ItemTypes = new[] { new { itemTypeId = "Product", baseType = "ItemTypeMemo" } }
            };

        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDependencyResolver>(s => new FuncDependencyResolver(s.GetRequiredService));
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddSingleton<IDocumentWriter, DocumentWriter>();
            ISchema schema = Schema.For(@"
  type Task {
    id: ID!
    itemTypes: [ItemType]
  }
  
  type ItemType {
    itemTypeId:String
    baseType:String
    value:ValueType!
  }
  
  type ValueType {
    value:String!
  }

  type NameType {
    nameTypeId:String
    rules:[Rule]!
    value:[NameTypeValue]
  }

  type NameTypeValue{
    unit:String!
    headings:[String!]
    nutrients:[Nutrient!]
    lookupId:Int!
    text:String!
    errors:[Error!]
  }


  type Rule{
    constraInts:[Int]
  }

  type Error{
    raisedBy:User
    raisedOn:String
    message:String
    resolvedBy:User!
    resolutionComment:String!
    resolvedOn:String!
    resolutionType:String!
  }

  type User{
    id:String
    name:String
  }

  type Nutrient{
    description:[String]
    values:[Float]
  }

  type Query {
    task: Task
  }

  input ItemTypeM {
    itemTypeId:String
    baseType:String
    value:ValueTypeM
  }

  input ValueTypeM {
    value:[String!]
    nameTypes:[NameTypeM!]
  }
  
  input NameTypeM {
    nameTypeId:String
    value:[NameTypeValueM]
  }

  input NameTypeValueM{
    unit:String!
    headings:[String!]
    nutrients:[NutrientM!]
    lookupId:Int!
    text:String!
  }

  input NutrientM{
    description:[String]
    values:[Float]
  }

  type Mutation {
    submitData(data: [ItemTypeM]):String
  }
"
                                        , _ => _.Types.Include<Query>());

            services.AddSingleton<ISchema>(schema);

            /*            services.AddSingleton<ISchema,StarWarsSchema>();
                        services.AddSingleton<StarWarsData>();
                        services.AddSingleton<StarWarsQuery>();
                        services.AddSingleton<StarWarsMutation>();
                        services.AddSingleton<HumanType>();
                        services.AddSingleton<HumanInputType>();
                        services.AddSingleton<DroidType>();
                        services.AddSingleton<CharacterInterface>();
                        services.AddSingleton<EpisodeEnum>();
            */
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddGraphQLHttp();
            services.Configure<ExecutionOptions>(options =>
            {
                options.EnableMetrics = true;
                options.ExposeExceptions = true;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseDeveloperExceptionPage();

            // add http for Schema at default url /graphql
            app.UseGraphQLHttp<ISchema>(new GraphQLHttpOptions());
            // use graphql-playground at default url /ui/playground
            app.UseGraphQLPlayground(new GraphQLPlaygroundOptions());
        }
    }
}

