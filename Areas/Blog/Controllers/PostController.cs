using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Models;
using App.Models.Blog;
using Microsoft.AspNetCore.Authorization;
using App.Data;
using Microsoft.AspNetCore.Identity;
using App.Utilities;
using AppMvc.Areas.Blog.Models;
using App.Models.UserManagement;

namespace App.Areas.Blog.Controllers
{
    [Area("Blog")]
    [Route("admin/blog/post/[action]/{id?}")]
    [Authorize(Roles = RoleName.Administrator + "," + RoleName.Editor)]
    public class PostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public PostController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        [TempData]
        public string StatusMessage { get; set; }

        // GET: Blog/Post
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int? currentPage, int? pagesize)
        {
            // Đảm bảo giá trị mặc định nếu tham số không được truyền vào
            int pageNumber = currentPage ?? 1;
            int pageSize = pagesize ?? 10;

            var posts = _context.Posts
                        .Include(p => p.Author)
                        .OrderByDescending(p => p.DateUpdated);

            // Tính toán tổng số bài viết
            int totalPosts = await posts.CountAsync();
            int countPages = (int)Math.Ceiling((double)totalPosts / pageSize);

            // Điều chỉnh số trang hợp lệ
            if (pageNumber > countPages) pageNumber = countPages;
            if (pageNumber < 1) pageNumber = 1;

            // Mô hình phân trang
            var pagingModel = new PagingModel()
            {
                countpages = countPages,
                currentpage = pageNumber,
                generateUrl = (pageNum) => Url.Action("Index", new
                {
                    p = pageNum,
                    pagesize = pageSize
                })
            };

            // Gán các giá trị cho ViewBag để sử dụng trong View
            ViewBag.pagingModel = pagingModel;
            ViewBag.totalPosts = totalPosts;
            ViewBag.postIndex = (pageNumber - 1) * pageSize;

            // Lấy các bài viết trong trang hiện tại
            var postsInPage = await posts.Skip((pageNumber - 1) * pageSize)
                                 .Take(pageSize)
                                 .Include(p => p.PostCategories)
                                 .ThenInclude(pc => pc.Category)
                                 .ToListAsync();

            return View(postsInPage);
        }


        // GET: Blog/Post/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Posts == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(m => m.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: Blog/Post/Create
        public async Task<IActionResult> CreateAsync()
        {
            var categories = await _context.Categories.ToListAsync();

            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");

            return View();
        }

        // POST: Blog/Post/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Slug,Content,Published,CategoryIDs")] CreatePostModel postModel)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");

            if (postModel.Slug == null)
            {
                postModel.Slug = AppUtilities.GenerateSlug(postModel.Title);
            }

            if (await _context.Posts.AnyAsync(p => p.Slug == postModel.Slug))
            {
                ModelState.AddModelError("Slug", "Nhập chuỗi Url khác");
                return View(postModel);
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(this.User);

                var post = new Post
                {
                    Title = postModel.Title,
                    Description = postModel.Description,
                    Slug = postModel.Slug,
                    Content = postModel.Content,
                    Published = postModel.Published,
                    AuthorId = user.Id,
                    DateCreated = DateTime.Now,
                    DateUpdated = DateTime.Now,
                };

                _context.Add(post);

                if (postModel.CategoryIDs != null)
                {
                    foreach (var CateId in postModel.CategoryIDs)
                    {
                        _context.Add(new PostCategory()
                        {
                            CategoryID = CateId,
                            Post = post
                        });
                    }
                }

                await _context.SaveChangesAsync();
                StatusMessage = "Vừa tạo bài viết mới";
                return RedirectToAction(nameof(Index));
            }

            return View(postModel);
        }


        // GET: Blog/Post/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // var post = await _context.Posts.FindAsync(id);
            var post = await _context.Posts.Include(p => p.PostCategories).FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            var postEdit = new CreatePostModel()
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                Description = post.Description,
                Slug = post.Slug,
                Published = post.Published,
                CategoryIDs = post.PostCategories.Select(pc => pc.CategoryID).ToArray()
            };

            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");

            return View(postEdit);
        }

        // POST: Blog/Post/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PostId,Title,Description,Slug,Content,Published,CategoryIDs")] CreatePostModel post)
        {
            if (id != post.PostId)
            {
                return NotFound();
            }
            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");


            if (post.Slug == null)
            {
                post.Slug = AppUtilities.GenerateSlug(post.Title);
            }

            if (await _context.Posts.AnyAsync(p => p.Slug == post.Slug && p.PostId != id))
            {
                ModelState.AddModelError("Slug", "Nhập chuỗi Url khác");
                return View(post);
            }


            if (ModelState.IsValid)
            {
                try
                {

                    var postUpdate = await _context.Posts.Include(p => p.PostCategories).FirstOrDefaultAsync(p => p.PostId == id);
                    if (postUpdate == null)
                    {
                        return NotFound();
                    }

                    postUpdate.Title = post.Title;
                    postUpdate.Description = post.Description;
                    postUpdate.Content = post.Content;
                    postUpdate.Published = post.Published;
                    postUpdate.Slug = post.Slug;
                    postUpdate.DateUpdated = DateTime.Now;

                    // Update PostCategory
                    if (post.CategoryIDs == null) post.CategoryIDs = new int[] { };

                    var oldCateIds = postUpdate.PostCategories.Select(c => c.CategoryID).ToArray();
                    var newCateIds = post.CategoryIDs;

                    var removeCatePosts = from postCate in postUpdate.PostCategories
                                          where (!newCateIds.Contains(postCate.CategoryID))
                                          select postCate;
                    _context.PostCategories.RemoveRange(removeCatePosts);

                    var addCateIds = from CateId in newCateIds
                                     where !oldCateIds.Contains(CateId)
                                     select CateId;

                    foreach (var CateId in addCateIds)
                    {
                        _context.PostCategories.Add(new PostCategory()
                        {
                            PostID = id,
                            CategoryID = CateId
                        });
                    }

                    _context.Update(postUpdate);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.PostId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                StatusMessage = "Vừa cập nhật bài viết";
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Id", post.AuthorId);
            return View(post);
        }


        // GET: Blog/Post/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(m => m.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Blog/Post/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            StatusMessage = "Bạn vừa xóa bài viết: " + post.Title;

            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.PostId == id);
        }
    }
}