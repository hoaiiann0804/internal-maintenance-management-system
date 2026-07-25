import { useMemo, useState } from "react";
import { wireframeData } from "../../shared/mock/wireframe-data";
import type {
  TicketHistoryItem,
  TicketPriority,
  TicketStatus,
} from "../../entities/ticket/model/types";
import { useTicketDetailQuery } from "../../features/tickets/api/use-ticket-detail-query";
import { useTicketsQuery } from "../../features/tickets/api/use-tickets-query";
import { CreateTicketModal } from "../../features/tickets/components/create-ticket-modal";
import { EditTicketModal } from "../../features/tickets/components/edit-ticket-modal";
import { TicketActionPanel } from "../../features/tickets/components/ticket-action-panel";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Search, Plus, Wrench, User, Calendar, Edit3, History } from "lucide-react";

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return "N/A";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeStyle: "short" }).format(
    new Date(value),
  );
};

export function TicketsPage() {
  const [search, setSearch] = useState("");
  const [ticketStatus, setTicketStatus] = useState<"All" | TicketStatus>("All");
  const [ticketPriority, setTicketPriority] = useState<"All" | TicketPriority>("All");
  const [selectedTicketId, setSelectedTicketId] = useState<number | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);

  const {
    data: ticketsPage,
    isLoading,
    isError,
  } = useTicketsQuery({
    status: ticketStatus === "All" ? undefined : ticketStatus,
    priority: ticketPriority === "All" ? undefined : ticketPriority,
  });

  const tickets = useMemo(() => ticketsPage?.items ?? [], [ticketsPage?.items]);
  const filteredTickets = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    if (!keyword) return tickets;

    return tickets.filter((ticket) =>
      [
        ticket.ticketCode,
        ticket.title,
        ticket.description,
        ticket.equipmentName,
        ticket.equipmentCode,
        ticket.createdByUserName,
      ]
        .filter(Boolean)
        .some((value) => value.toLowerCase().includes(keyword)),
    );
  }, [search, tickets]);

  const activeTicketId = useMemo(() => {
    if (selectedTicketId && filteredTickets.some((ticket) => ticket.id === selectedTicketId)) {
      return selectedTicketId;
    }
    return filteredTickets[0]?.id ?? null;
  }, [selectedTicketId, filteredTickets]);

  const {
    data: selectedTicket,
    isLoading: isSelectedTicketLoading,
    isError: isSelectedTicketError,
  } = useTicketDetailQuery(activeTicketId);

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Resolved":
      case "Closed":
        return <Badge variant="success">{status}</Badge>;
      case "InProgress":
        return <Badge variant="default">{status}</Badge>;
      case "Assigned":
        return <Badge variant="warning">{status}</Badge>;
      case "Cancelled":
        return <Badge variant="outline">{status}</Badge>;
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  const getPriorityBadge = (priority: string) => {
    switch (priority) {
      case "Critical":
      case "High":
        return <Badge variant="destructive">{priority}</Badge>;
      case "Medium":
        return <Badge variant="warning">{priority}</Badge>;
      default:
        return <Badge variant="outline">{priority}</Badge>;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-card border rounded-2xl p-6 shadow-sm">
        <div>
          <span className="text-xs font-semibold text-primary uppercase tracking-wider">
            Workspace
          </span>
          <h1 className="text-2xl font-bold tracking-tight text-foreground mt-1">Ticket Queue</h1>
          <p className="text-xs text-muted-foreground mt-1">
            Quản lý, phân công và theo dõi tiến độ giải quyết sự cố thiết bị nội bộ.
          </p>
        </div>
        <Button onClick={() => setIsCreateModalOpen(true)} className="gap-1.5 shrink-0">
          <Plus className="h-4 w-4" />
          <span>Tạo Ticket Mới</span>
        </Button>
      </div>

      {/* Main 2-Column Split View */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Left Column: Master Queue List (5 cols) */}
        <Card className="lg:col-span-5 flex flex-col max-h-[850px]">
          <CardHeader className="pb-3 border-b">
            <CardTitle className="text-base font-semibold">Danh sách Ticket</CardTitle>
            <CardDescription className="text-xs">
              Tổng cộng {filteredTickets.length} ticket trong bộ lọc hiện tại
            </CardDescription>

            {/* Filter Bar */}
            <div className="space-y-3 pt-2">
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
                <Input
                  type="text"
                  value={search}
                  placeholder="Tìm mã ticket, tiêu đề, thiết bị..."
                  onChange={(e) => setSearch(e.target.value)}
                  className="pl-8 text-xs h-8"
                />
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div className="space-y-1">
                  <Label className="text-[11px] text-muted-foreground">Trạng thái</Label>
                  <select
                    className="flex h-8 w-full rounded-md border border-input bg-transparent px-2 text-xs shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    value={ticketStatus}
                    onChange={(e) => setTicketStatus(e.target.value as TicketStatus | "All")}
                  >
                    {["All", ...wireframeData.workflow, "Cancelled"].map((status) => (
                      <option key={status} value={status} className="bg-background text-foreground">
                        {status}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1">
                  <Label className="text-[11px] text-muted-foreground">Độ ưu tiên</Label>
                  <select
                    className="flex h-8 w-full rounded-md border border-input bg-transparent px-2 text-xs shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    value={ticketPriority}
                    onChange={(e) => setTicketPriority(e.target.value as TicketPriority | "All")}
                  >
                    {["All", ...wireframeData.priorities].map((p) => (
                      <option key={p} value={p} className="bg-background text-foreground">
                        {p}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            </div>
          </CardHeader>

          <CardContent className="p-2 overflow-y-auto flex-1 divide-y">
            {isLoading ? (
              <div className="py-12 flex justify-center">
                <div className="h-6 w-6 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
              </div>
            ) : isError ? (
              <div className="p-6 text-center text-xs text-destructive">
                Không thể tải danh sách ticket. Vui lòng thử lại sau.
              </div>
            ) : filteredTickets.length > 0 ? (
              filteredTickets.map((ticket) => {
                const isSelected = ticket.id === activeTicketId;
                return (
                  <button
                    key={ticket.id}
                    type="button"
                    onClick={() => setSelectedTicketId(ticket.id)}
                    className={`w-full text-left p-3 rounded-lg transition-all space-y-1.5 ${
                      isSelected
                        ? "bg-primary/10 border-l-4 border-l-primary"
                        : "hover:bg-accent/60"
                    }`}
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-xs font-mono font-bold text-primary">
                        {ticket.ticketCode}
                      </span>
                      <div className="flex items-center gap-1.5">
                        {getPriorityBadge(ticket.priority)}
                        {getStatusBadge(ticket.status)}
                      </div>
                    </div>

                    <h4 className="text-xs font-semibold text-foreground line-clamp-1">
                      {ticket.title}
                    </h4>

                    <div className="flex items-center justify-between text-[11px] text-muted-foreground pt-0.5">
                      <span className="truncate max-w-[160px] flex items-center gap-1">
                        <Wrench className="h-3 w-3 shrink-0" />
                        {ticket.equipmentName}
                      </span>
                      <span className="shrink-0">{formatDateTime(ticket.createdAt)}</span>
                    </div>
                  </button>
                );
              })
            ) : (
              <div className="p-8 text-center text-xs text-muted-foreground">
                Không tìm thấy ticket nào phù hợp.
              </div>
            )}
          </CardContent>
        </Card>

        {/* Right Column: Ticket Detail View (7 cols) */}
        <div className="lg:col-span-7 space-y-6">
          {isSelectedTicketLoading ? (
            <Card>
              <CardContent className="py-16 flex justify-center">
                <div className="h-7 w-7 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
              </CardContent>
            </Card>
          ) : isSelectedTicketError ? (
            <Card className="border-destructive/30">
              <CardContent className="py-12 text-center text-xs text-destructive">
                Không thể tải chi tiết ticket này.
              </CardContent>
            </Card>
          ) : selectedTicket ? (
            <>
              {/* Selected Ticket Main Card */}
              <Card>
                <CardHeader className="pb-4 border-b">
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-mono font-bold text-primary bg-primary/10 px-2 py-0.5 rounded-md border border-primary/20">
                          {selectedTicket.ticketCode}
                        </span>
                        {getStatusBadge(selectedTicket.status)}
                        {getPriorityBadge(selectedTicket.priority)}
                      </div>
                      <h2 className="text-xl font-bold tracking-tight text-foreground mt-2">
                        {selectedTicket.title}
                      </h2>
                    </div>

                    {(selectedTicket.status === "Pending" ||
                      selectedTicket.status === "Assigned") && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setIsEditModalOpen(true)}
                        className="gap-1.5 shrink-0"
                      >
                        <Edit3 className="h-3.5 w-3.5" />
                        <span>Chỉnh sửa</span>
                      </Button>
                    )}
                  </div>

                  <div className="flex flex-wrap items-center gap-4 text-xs text-muted-foreground pt-3">
                    <span className="flex items-center gap-1.5">
                      <User className="h-3.5 w-3.5 text-primary" />
                      Tạo bởi:{" "}
                      <strong className="text-foreground">
                        {selectedTicket.createdByUserName}
                      </strong>
                    </span>
                    <span>•</span>
                    <span className="flex items-center gap-1.5">
                      <Calendar className="h-3.5 w-3.5 text-primary" />
                      {formatDateTime(selectedTicket.createdAt)}
                    </span>
                  </div>
                </CardHeader>

                <CardContent className="p-6 space-y-4">
                  {/* Description Box */}
                  <div className="space-y-1.5">
                    <h4 className="text-xs font-semibold text-muted-foreground uppercase">
                      Mô tả sự cố
                    </h4>
                    <div className="p-3.5 rounded-xl border bg-muted/20 text-xs leading-relaxed whitespace-pre-wrap">
                      {selectedTicket.description}
                    </div>
                  </div>

                  {/* Info Cards Grid */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
                    <div className="p-3 rounded-lg border bg-card space-y-1">
                      <p className="text-[11px] font-semibold text-muted-foreground uppercase flex items-center gap-1">
                        <Wrench className="h-3.5 w-3.5 text-blue-500" /> Thiết bị
                      </p>
                      <p className="text-xs font-semibold text-foreground">
                        {selectedTicket.equipmentCode} — {selectedTicket.equipmentName}
                      </p>
                    </div>

                    <div className="p-3 rounded-lg border bg-card space-y-1">
                      <p className="text-[11px] font-semibold text-muted-foreground uppercase flex items-center gap-1">
                        <User className="h-3.5 w-3.5 text-amber-500" /> Kỹ thuật viên phụ trách
                      </p>
                      <p className="text-xs font-semibold text-foreground">
                        {selectedTicket.assignedTechnicianName ?? "Chưa phân công"}
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Ticket Action Panel (Assign, Transition Status, Attachments, Comments) */}
              <TicketActionPanel ticket={selectedTicket} />

              {/* Timeline Card */}
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm font-semibold flex items-center gap-2">
                    <History className="h-4 w-4 text-primary" />
                    <span>Lịch sử thay đổi (Timeline)</span>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="relative border-l border-muted pl-4 space-y-4 ml-2">
                    {selectedTicket.history.map((item: TicketHistoryItem) => (
                      <div key={item.id} className="relative">
                        <div className="absolute -left-[21px] top-1.5 w-2.5 h-2.5 rounded-full bg-primary ring-4 ring-background" />
                        <div className="text-xs space-y-0.5">
                          <p className="font-semibold text-foreground">{item.status}</p>
                          <p className="text-muted-foreground">
                            {item.note && <span className="text-foreground">{item.note} · </span>}
                            <span>{item.changedBy}</span> ·{" "}
                            <span>{formatDateTime(item.changedAt)}</span>
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </>
          ) : (
            <Card>
              <CardContent className="py-16 text-center text-xs text-muted-foreground">
                Vui lòng chọn một ticket từ danh sách bên trái để xem chi tiết.
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      <CreateTicketModal isOpen={isCreateModalOpen} onClose={() => setIsCreateModalOpen(false)} />
      {isEditModalOpen && selectedTicket && (
        <EditTicketModal
          key={selectedTicket.id}
          ticket={selectedTicket}
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
        />
      )}
    </div>
  );
}
