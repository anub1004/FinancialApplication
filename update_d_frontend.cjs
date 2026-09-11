const fs = require("fs");
const path = require("path");

const sidebarPath = "D:\\FinancialApllication_Frontend\\Frontend\\src\\Component\\partials\\Sidebar.jsx";
let sidebarContent = fs.readFileSync(sidebarPath, "utf8");

if (!sidebarContent.includes('to="/admin/users"')) {
  const marker = 'to="/admin/subscriptions"';
  const idx = sidebarContent.indexOf(marker);
  if (idx !== -1) {
    const endLiIdx = sidebarContent.indexOf("</li>", idx);
    if (endLiIdx !== -1) {
      const insertionPoint = endLiIdx + 5;
      const userManagementBlock = `\n                            <li className="mb-1 last:mb-0">
                              <NavLink
                                end
                                to="/admin/users"
                                className={({ isActive }) =>
                                  "block transition duration-150 truncate " + (isActive ? "text-violet-500" : "text-gray-500/90 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200")
                                }
                              >
                                <span className="text-sm font-medium lg:opacity-0 lg:sidebar-expanded:opacity-100 2xl:opacity-100 duration-200">
                                  User Management
                                </span>
                              </NavLink>
                            </li>`;

      sidebarContent = sidebarContent.slice(0, insertionPoint) + userManagementBlock + sidebarContent.slice(insertionPoint);
      fs.writeFileSync(sidebarPath, sidebarContent, "utf8");
      console.log("Successfully inserted User Management link into Sidebar.jsx");
    }
  }
} else {
  console.log("User Management already in Sidebar.jsx");
}
