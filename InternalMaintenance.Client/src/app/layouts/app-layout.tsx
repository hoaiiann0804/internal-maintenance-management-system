import { Outlet, Link, useLocation, useNavigate } from "react-router-dom";
import { Toaster } from "sonner";
import { useAuthStore } from "@/features/auth/model/auth-store";
import { appRoutes } from "@/shared/config/routes";
import { logout } from "@/shared/api/auth";
import { UserProfile, ThemeToggle } from "@/shared/ui";
import { useState } from "react";
import { ChangePasswordModal } from "@/features/auth/components/change-password-modal";
import { LayoutDashboard, Ticket, Wrench, Users, Building2, Store, BarChart3 } from "lucide-react";
import { cn } from "@/lib/utils";

export function AppLayout() {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);
  const location = useLocation();
  const navigate = useNavigate();
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);

  const isAdmin = session?.user.roleName === "Admin";
  const isManagerOrAdmin = isAdmin || session?.user.roleName === "Manager";
  const isLoginPage = location.pathname === appRoutes.login || !session;

  const handleLogout = async (): Promise<void> => {
    const refreshToken = session?.refreshToken;
    try {
      if (refreshToken) {
        await logout(refreshToken);
      }
    } finally {
      signOut();
      navigate(appRoutes.login);
    }
  };

  const navItems = [
    { to: appRoutes.dashboard, label: "Dashboard", icon: LayoutDashboard },
    { to: appRoutes.tickets, label: "Tickets", icon: Ticket },
    { to: appRoutes.equipment, label: "Equipment", icon: Wrench },
    ...(isManagerOrAdmin ? [{ to: appRoutes.vendors, label: "Vendors", icon: Store }] : []),
    ...(isManagerOrAdmin ? [{ to: appRoutes.reports, label: "Reports", icon: BarChart3 }] : []),
    ...(isAdmin ? [{ to: appRoutes.users, label: "Users", icon: Users }] : []),
    ...(isAdmin ? [{ to: appRoutes.departments, label: "Departments", icon: Building2 }] : []),
  ];

  return (
    <div className="min-h-screen bg-background text-foreground flex flex-col relative">
      <Toaster
        position="top-right"
        richColors
        duration={3500}
        visibleToasts={3}
        toastOptions={{ style: { borderRadius: "12px" } }}
      />

      {isLoginPage ? (
        /* Minimal layout for Login page — Topbar hidden, floating ThemeToggle only */
        <div className="absolute top-4 right-4 z-50">
          <ThemeToggle />
        </div>
      ) : (
        /* Authenticated Top Navbar Header */
        <header className="sticky top-0 z-40 w-full border-b bg-background/80 backdrop-blur-md">
          <div className="max-w-7xl mx-auto flex h-16 items-center justify-between px-4 sm:px-6 lg:px-8">
            {/* Brand logo */}
            <div className="flex items-center gap-3">
              <Link to={appRoutes.dashboard} className="flex items-center gap-2.5 group">
                <div className="w-9 h-9 rounded-xl bg-primary flex items-center justify-center font-bold text-primary-foreground text-xs shadow-sm transition-transform group-hover:scale-105">
                  IM
                </div>
                <div className="hidden md:flex flex-col">
                  <span className="text-sm font-bold leading-none tracking-tight">
                    Management Console
                  </span>
                  <span className="text-[10px] text-muted-foreground leading-none mt-1">
                    Facility Maintenance
                  </span>
                </div>
              </Link>
            </div>

            {/* Navigation Links */}
            <nav className="flex items-center space-x-1 sm:space-x-2">
              {navItems.map((item) => {
                const Icon = item.icon;
                const isActive =
                  location.pathname === item.to ||
                  (item.to !== appRoutes.dashboard && location.pathname.startsWith(item.to));
                return (
                  <Link
                    key={item.to}
                    to={item.to}
                    className={cn(
                      "flex items-center gap-2 px-3 py-2 rounded-lg text-xs font-medium transition-all relative",
                      isActive
                        ? "text-primary bg-primary/10 font-semibold"
                        : "text-muted-foreground hover:text-foreground hover:bg-accent",
                    )}
                  >
                    <Icon
                      className={cn("h-4 w-4", isActive ? "text-primary" : "text-muted-foreground")}
                    />
                    <span className="hidden sm:inline">{item.label}</span>
                  </Link>
                );
              })}
            </nav>

            {/* User profile dropdown & theme toggle */}
            <UserProfile
              fullName={session?.user.fullName}
              roleName={session?.user.roleName}
              onLogout={handleLogout}
              onChangePassword={() => setIsChangePasswordOpen(true)}
            />
          </div>
        </header>
      )}

      {/* Main Content Area */}
      <main className={cn("flex-1 w-full", !isLoginPage && "max-w-7xl mx-auto p-4 sm:p-6 lg:p-8")}>
        <Outlet />
      </main>

      <ChangePasswordModal
        isOpen={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </div>
  );
}
