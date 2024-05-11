using System;

using System.Linq;
using System.Web;
using System.Web.Mvc;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.DynamicModules.Model;
using Telerik.Sitefinity.Model;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.Mvc;
using Telerik.Sitefinity.Utilities.TypeConverters;

using TrialProject.Mvc.Models;
using Image = Telerik.Sitefinity.Libraries.Model.Image;

using System.IO;
using System.Text.RegularExpressions;
using Telerik.Sitefinity.Workflow;
using Telerik.Sitefinity.Taxonomies.Model;
using Telerik.Sitefinity.Taxonomies;

using Telerik.Sitefinity;

using Album = Telerik.Sitefinity.Libraries.Model.Album;
using Telerik.Sitefinity.RelatedData;
using Guid = System.Guid;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telerik.Sitefinity.Modules.Blogs;
using System.Net.Http;
using Telerik.Sitefinity.Blogs.Model;

namespace TrialProject.Mvc.Controllers
{
    [ControllerToolboxItem(Name = "MessageWidget", Title = "Message Widget", SectionName = "MvcWidgets")]
    public class MessageWidgetController : Controller
    {
        // here we create a global variable for storing the album id , that can be accessible anywhere
        Guid albumId;
        string albumTitle;

        private static readonly HttpClient client = new HttpClient();

        //GET: MessageWidget
        public ActionResult Index()
        { 
            return View();
        }

        public async Task<List<BlogPostData>> getData()

        {

            string apiUrl = "https://www.pharmacyitk.com.au/wp-json/wp/v2/posts";

            List<BlogPostData> dataObj = new List<BlogPostData>();

            HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)

            {

                string jsonResponse = await response.Content.ReadAsStringAsync(); ;

                dataObj = JsonConvert.DeserializeObject<List<BlogPostData>>(jsonResponse);

            }

            return retriveData(dataObj);

        }

        private List<BlogPostData> retriveData(List<BlogPostData> dataObj)

        {

            BlogsManager blogsManager = BlogsManager.GetManager();

            Blog blog = blogsManager.GetBlogs().Where(b => b.Title == "test1").FirstOrDefault();




            foreach (var post in dataObj)

            {

                Guid Id = Guid.NewGuid();

              

                createBlogPost(Id, post, blog);

            }



            throw new NotImplementedException();

        }


        //private async Task<List<BlogPostData>> createTagsPost()
        //{
        //    string apiUrl = "https://www.pharmacyitk.com.au/wp-json/wp/v2/tags?post=29873";

        //    List<BlogPostData> dataObj = new List<BlogPostData>();

        //    HttpClient client = new HttpClient();

        //    HttpResponseMessage response = await client.GetAsync(apiUrl);

        //    if (response.IsSuccessStatusCode)

        //    {

        //        string jsonResponse = await response.Content.ReadAsStringAsync(); ;

        //        dataObj = JsonConvert.DeserializeObject<List<BlogPostData>>(jsonResponse);

        //    }

        //    return retriveTags(dataObj);
        //}

        //private List<BlogPostData> retriveTags(List<BlogPostData> dataObj) 
        //{
        //    foreach(var post in dataObj)
        //    {

        //    }

        //    throw new NotImplementedException();
        //}

        private void createBlogPost(Guid id, BlogPostData post, Blog blogpost)

        {

            BlogsManager blogsManager = BlogsManager.GetManager();

            BlogPost blogPost = blogsManager.GetBlogPosts().Where(item => item.Title == post.title.rendered).FirstOrDefault();

            //string tags = post.Tags;

            //string category = post.Category;


            if (blogPost == null)

            {

                //The Blogs item is created as a master. The masterBlogPostId is assigned to the master.

                blogPost = blogsManager.CreateBlogPost(id);

                //Set the parent blog.

                Blog blog = blogsManager.GetBlogs().Where(b => b.Id == blogpost.Id).SingleOrDefault();

                blogPost.Parent = blog;

                //Set the properties of the blog post.                

                blogPost.Title = post.title.rendered;

                blogPost.Content = post.content.rendered;

                blogPost.DateCreated = DateTime.UtcNow;

                blogPost.PublicationDate = DateTime.UtcNow;

                blogPost.LastModified = DateTime.UtcNow;

                blogPost.UrlName = Regex.Replace(blogPost.Title.ToLower(), @"[^\w\-\!\$\'\(\)\=\@\d_]+", "-");


                //Recompiles and validates the url of the blog.

                blogsManager.RecompileAndValidateUrls(blogPost);


                //    // adding tagging to the library

                //    //addTags(tags);

                //    //addTaxon(blogPost, tags);


                //    //adding category to the library

                //    //addCategory(category);

                //    //addCategories(blogPost, category);


                //Save the changes.

                blogsManager.SaveChanges();


                //Publish the Blogs item. The published version acquires new ID.

                var bag = new Dictionary<string, string>();

                bag.Add("ContentType", typeof(BlogPost).FullName);

                WorkflowManager.MessageWorkflow(id, typeof(BlogPost), null, "Publish", false, bag);


            }

        }

        //Main function, it handles the submission of form detail .

        [HttpPost]
        public ActionResult SubmitForm(MessageWidgetModel messageWidgetModel, HttpPostedFileBase ItemImage)
        {
           


            // here we store the value of Tags
            string name = messageWidgetModel.Tags;


            // here we store the value of Category
            string category = messageWidgetModel.Category;




            // Get an instance of the DynamicModuleManager to manage dynamic content
            DynamicModuleManager dynamicModuleManager = DynamicModuleManager.GetManager();


            // Resolve the type of the custom dynamic module named "Kanishk"
            Type kanishkType = TypeResolutionService.ResolveType("Telerik.Sitefinity.DynamicTypes.Model.DynamicModule.Kanishk");


            // Create a new data item (content item) of the "Kanishk" dynamic module type
            DynamicContent kanishkItem = dynamicModuleManager.CreateDataItem(kanishkType);



            // Set the "Title" field of the dynamic content item to the value of messageWidgetModel.Title

            kanishkItem.SetString("Title", messageWidgetModel.Title);

            // Set the "Description" field of the dynamic content item to the value of messageWidgetModel.Description
            kanishkItem.SetString("Description", messageWidgetModel.Description);









            // Add Tags Method
            AddTags(name);

            addtaxon(kanishkItem, messageWidgetModel.Tags);


            // Add Category Method
            addCategory(category);
            addCategoryToModule(kanishkItem, category);


            // Album Method
            FindFolderById();


            if (ItemImage != null && ItemImage.ContentLength > 0)
            {


                // Generate a new GUID for the image
                Guid masterImageId = Guid.NewGuid();
                // Obtain the image stream from the uploaded file
                Stream imageStream = ItemImage.InputStream;

                // Obtain the image file name
                string imageFileName = System.IO.Path.GetFileName(ItemImage.FileName);

                // Obtain the image extension from the file name
                string imageExtension = System.IO.Path.GetExtension(imageFileName);


                string imageTitle = System.IO.Path.GetFileNameWithoutExtension(imageFileName);

                // call the create image function
                CreateImageWithNativeAPI(masterImageId, imageFileName, imageExtension, imageTitle, imageStream, albumId, kanishkItem);



            }


            // publishing the item into dynamic module records
            dynamicModuleManager.Lifecycle.Publish(kanishkItem);
            kanishkItem.SetWorkflowStatus(dynamicModuleManager.Provider.ApplicationName, "Published");
            dynamicModuleManager.SaveChanges();

            getData();


            return View("Index", messageWidgetModel);

        }


        // Add tag 
        private void AddTags(string name)
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

        public void addtaxon(DynamicContent kanishkItem, string tagname)
        {
            TaxonomyManager taxonomyManager = TaxonomyManager.GetManager();
            var Tags = taxonomyManager.GetTaxa<FlatTaxon>().Where(t => t.Taxonomy.Name == "Tags");

            foreach (var Tag in Tags.Where(w => w.Title.ToLower() == tagname.ToLower()))
            {
                if (Tag != null)
                {
                    kanishkItem.Organizer.AddTaxa("Tags", Tag.Id);
                }
            }
        }


        // Add Category
        public void addCategory(string category)
        {
            var taxonomyManager = TaxonomyManager.GetManager();

            //Get the Categories taxonomy
            var categoryTaxonomy = taxonomyManager.GetTaxonomies<HierarchicalTaxonomy>().SingleOrDefault(s => s.Name == "Categories");

            if (categoryTaxonomy == null) return;

            //Create a new HierarchicalTaxon
            var taxon = taxonomyManager.CreateTaxon<HierarchicalTaxon>();

            //Associate the item with the hierarchical taxonomy
            taxon.Taxonomy = categoryTaxonomy;

            taxon.Name = Regex.Replace(category.ToLower(), @"[^\w\-\!\$\'\(\)\=\@\d_]+", "-");
            taxon.Title = category;
            taxon.UrlName = Regex.Replace(category.ToLower(), @"[^\w\-\!\$\'\(\)\=\@\d_]+", "-");



            //Add it to the list
            categoryTaxonomy.Taxa.Add(taxon);

            taxonomyManager.SaveChanges();
        }

        public void addCategoryToModule(DynamicContent kanishkItem, string categoryName)
        {
            TaxonomyManager taxonomyManager = TaxonomyManager.GetManager();
            var Category = taxonomyManager.GetTaxa<HierarchicalTaxon>().Where(t => t.Taxonomy.Name == "Categories");

            foreach (var categorys in Category.Where(w => w.Title.ToLower() == categoryName.ToLower()))
            {

                if (categorys != null)
                {
                    kanishkItem.Organizer.AddTaxa("Category", categorys.Id);
                }
            }
        }

        // create Album id 
        public void FindFolderById()
        {
            //gets an isntance of the LibrariesManager
            var manager = LibrariesManager.GetManager();
            Album albumManager = manager.GetAlbums().Where(a => a.Title == "ImageAlbumTitle1").FirstOrDefault();
            //creates an image album(library)

            if (albumManager == null)
            {


                var imagesAlbum = manager.CreateAlbum();
                imagesAlbum.Title = "ImageAlbumTitle3";
                manager.SaveChanges();
            }
            else
            {
                CreateAlbumNativeAPI(albumManager);
            }


            //Creating an album with predefined ID


        }

        private void CreateAlbumNativeAPI(Album imagesAlbum)
        {
            albumId = imagesAlbum.Id;
            albumTitle = imagesAlbum.Title;
        }



        //Create images

        private void CreateImageWithNativeAPI(Guid masterImageId, string imageFileName, string imageExtension, string imageTitle, Stream imageStream, Guid albumId, DynamicContent kanishkItem)
        {
            LibrariesManager librariesManager = LibrariesManager.GetManager();
            Image image = librariesManager.GetImages().Where(i => i.Id == masterImageId).FirstOrDefault();


            if (image == null)
            {
                //The album post is created as master. The masterImageId is assigned to the master version.
                image = librariesManager.CreateImage(masterImageId);

                //Set the parent album.
                Album album = librariesManager.GetAlbums().Where(i => i.Id == albumId).SingleOrDefault();
                image.Parent = album;

                //Set the properties of the album post.
                image.Title = imageTitle;
                image.DateCreated = DateTime.UtcNow;
                image.PublicationDate = DateTime.UtcNow;
                image.LastModified = DateTime.UtcNow;
                image.UrlName = Regex.Replace(imageTitle.ToLower(), @"[^\w\-\!\$\'\(\)\=\@\d_]+", "-");
                image.MediaFileUrlName = Regex.Replace(imageFileName.ToLower(), @"[^\w\-\!\$\'\(\)\=\@\d_]+", "-");

                //Upload the image file.
                // The imageExtension parameter must contain '.', for example '.jpeg'
                librariesManager.Upload(image, imageStream, imageExtension);

                //Save the changes.
                librariesManager.SaveChanges();

                ////Publish the Albums item. The live version acquires new ID.
                //var bag = new Dictionary<string, string>();
                //bag.Add("ContentType", typeof(Image).FullName);
                //WorkflowManager.MessageWorkflow(masterImageId, typeof(Image), null, "Publish", false, bag);


                var itemImageItem = librariesManager.GetImages().FirstOrDefault(i => i.Status == Telerik.Sitefinity.GenericContent.Model.ContentLifecycleStatus.Master);
                if (itemImageItem != null)
                {
                    // This is how we relate an item
                    kanishkItem.CreateRelation(itemImageItem, "ItemImage");
                }
            }
        }
    }
}
