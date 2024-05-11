using Newtonsoft.Json;
using ServiceStack.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using Telerik.Sitefinity.Blogs.Model;
using Telerik.Sitefinity.Frontend.Blogs.Mvc.Models.BlogPost;
using Telerik.Sitefinity.Modules.Blogs;
using Telerik.Sitefinity.Mvc;
using Telerik.Sitefinity.Taxonomies.Model;
using Telerik.Sitefinity.Taxonomies;
using Telerik.Sitefinity.Workflow;
using TrialProject.Mvc.Models;

namespace TrialProject.Mvc.Controllers
{
    [ControllerToolboxItem(Name = "ApiWidget", Title = "Api Widget", SectionName = "ApiWidgets")]
    public class ApiWidgetController : Controller
    {

        Uri baseAddress = new Uri("https://www.pharmacyitk.com.au/wp-json/wp/v2/posts");
        private readonly HttpClient _httpClient;

        public ApiWidgetController()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = baseAddress;
        }
        [HttpGet]
        public ActionResult Index()
        
        {
            List<Post.Class1> posts = new List<Post.Class1>();
            HttpResponseMessage response = _httpClient.GetAsync(_httpClient.BaseAddress).Result;
            if (response.IsSuccessStatusCode)
            {

                string data = response.Content.ReadAsStringAsync().Result;
                posts = JsonConvert.DeserializeObject<List<Post.Class1>>(data);
            }


            foreach (var post in posts)
            {

                int postId = post.id;

                // Call the function to fetch tags
                int[] tags = post.tags;

                //GetTagsAsync(postId);
                BlogPost(post);
            }
            // Return the view with the fetched data
            return View(posts);
        }


        public ActionResult GetTagsAsync(int postId, BlogPost blogPost)
        {
            List<Tag.Class1> tags = new List<Tag.Class1>();

            try
            {
                // Construct the URL with the post ID
                string tagsUrl = $"https://www.pharmacyitk.com.au/wp-json/wp/v2/tags?post=" + postId;

                HttpResponseMessage tagsResponse = _httpClient.GetAsync(tagsUrl).Result;
                if (tagsResponse.IsSuccessStatusCode)
                {
                    string tagsData = tagsResponse.Content.ReadAsStringAsync().Result;
                    tags = JsonConvert.DeserializeObject<List<Tag.Class1>>(tagsData);
                }
                foreach (var tag in tags)
                {
                    int tagId = tag.id;
                    AddTags(tag.name , tag);
                    addtaxon(blogPost, tag.name);
                }
            }
            catch (Exception ex)
            {

            }


            return View(tags);
        }




        public class Tag
        {



            public class Class1
            {
                public int id { get; set; }
                public int count { get; set; }
                public string description { get; set; }
                public string link { get; set; }
                public string name { get; set; }
                public string slug { get; set; }
                public string taxonomy { get; set; }
                public object[] meta { get; set; }
                public _Links _links { get; set; }
            }

            public class _Links
            {
                public Self[] self { get; set; }
                public Collection[] collection { get; set; }
                public About[] about { get; set; }
                public WpPost_Type[] wppost_type { get; set; }
                public Cury[] curies { get; set; }
            }

            public class Self
            {
                public string href { get; set; }
            }

            public class Collection
            {
                public string href { get; set; }
            }

            public class About
            {
                public string href { get; set; }
            }

            public class WpPost_Type
            {
                public string href { get; set; }
            }

            public class Cury
            {
                public string name { get; set; }
                public string href { get; set; }
                public bool templated { get; set; }
            }

            // Add other properties as needed
        }

        public ActionResult BlogPost(Post.Class1 post)
        {
           



            BlogsManager blogsManager = BlogsManager.GetManager();
            Blog blog = blogsManager.GetBlogs().Where(b => b.Title == "newblog").SingleOrDefault();

            System.Guid masterBlogPostId = System.Guid.NewGuid();


            CreateBlogPostNativeAPI(masterBlogPostId,  blog , post);

            return View("Index");
        }

        private void CreateBlogPostNativeAPI(System.Guid masterBlogPostId, Blog blogpost, Post.Class1 post)
        {
            BlogsManager blogsManager = BlogsManager.GetManager();
            BlogPost blogPost =  blogsManager.GetBlogPosts().Where(item=>item.Title == post.title.rendered).FirstOrDefault();

            if (blogPost == null)
            {
                //The Blogs item is created as a master. The masterBlogPostId is assigned to the master.
                blogPost = blogsManager.CreateBlogPost(masterBlogPostId);

                //Set the parent blog.
                Blog blog = blogsManager.GetBlogs().Where(b => b.Id == blogpost.Id).SingleOrDefault();

                blogPost.Parent = blog;

                // Here I am calling the Get Tag function
                int postId = post.id;

                GetTagsAsync(postId , blogPost);


                //add image in blog


                //Set the properties of the blog post.                
                blogPost.Title = post.title.rendered;
                blogPost.Content = post.content.rendered;
                blogPost.DateCreated = DateTime.UtcNow;
                blogPost.PublicationDate = DateTime.UtcNow;
                blogPost.LastModified = DateTime.UtcNow;
                blogPost.UrlName = Regex.Replace(blogPost.Title.ToLower(), @"[^\w\-\!\$\'\(\)\=\@\d_]+", "-");

                //Recompiles and validates the url of the blog.
                blogsManager.RecompileAndValidateUrls(blogPost);

                //Save the changes.
                blogsManager.SaveChanges();

                //Publish the Blogs item. The published version acquires new ID.
                var bag = new Dictionary<string, string>();
                bag.Add("ContentType", typeof(BlogPost).FullName);
                WorkflowManager.MessageWorkflow(masterBlogPostId, typeof(BlogPost), null, "Publish", false, bag);
            }
        }

        private void AddTags(string name, Tag.Class1 tag)
        {

            var taxonomyManager = TaxonomyManager.GetManager();

            //Get the Tags taxonomy
            var tagTaxonomy = taxonomyManager.GetTaxonomies<FlatTaxonomy>().SingleOrDefault(s => s.Name == "Tags");

            if (tagTaxonomy == null) return;

            //Create a new FlatTaxon
            var taxon = taxonomyManager.CreateTaxon<FlatTaxon>();

            //Associate the item with the flat taxonomy
            taxon.FlatTaxonomy = tagTaxonomy;

            taxon.Name = Regex.Replace(name.ToLower(), @"[^\w\-\!\$\'\(\)\=\@\d_]+", "-");
            taxon.Title = name;
            taxon.UrlName = Regex.Replace(name.ToLower(), @"[^\w\-\!\$\'\(\)\=\@\d_]+", "-");

            //Add it to the list
            tagTaxonomy.Taxa.Add(taxon);
            taxonomyManager.SaveChanges();

        }



        public void addtaxon(BlogPost blogPost, string tagname)
        {
            TaxonomyManager taxonomyManager = TaxonomyManager.GetManager();
            var Tags = taxonomyManager.GetTaxa<FlatTaxon>().Where(t => t.Taxonomy.Name == "Tags");

            foreach (var Tag in Tags.Where(w => w.Title.ToLower() == tagname.ToLower()))
            {
                if (Tag != null)
                {
                    blogPost.Organizer.AddTaxa("Tags", Tag.Id);
                }
            }
        }





    }
}