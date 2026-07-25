import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import type { TicketAttachment } from "../../../entities/ticket/model/types";
import { getAttachmentDownloadUrl } from "../../../shared/api/attachments";
import { useDeleteAttachmentMutation } from "../api/use-delete-attachment-mutation";
import { Button } from "@/components/ui/button";
import { FileText, Image as ImageIcon, Video, Trash2, Download, Loader2 } from "lucide-react";

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(dateStr: string): string {
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(dateStr));
}

function getFileIcon(fileType: TicketAttachment["fileType"]) {
  switch (fileType) {
    case "Image":
      return <ImageIcon className="h-4 w-4 text-blue-500" />;
    case "Video":
      return <Video className="h-4 w-4 text-purple-500" />;
    case "Document":
    default:
      return <FileText className="h-4 w-4 text-amber-500" />;
  }
}

type Props = {
  ticketId: number;
  attachments: TicketAttachment[];
  canDelete: boolean;
  currentUserId: number;
};

export function AttachmentList({ ticketId, attachments, canDelete, currentUserId }: Props) {
  const [openingId, setOpeningId] = useState<number | null>(null);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  const deleteMutation = useDeleteAttachmentMutation(ticketId);

  const handleOpen = async (attachment: TicketAttachment) => {
    if (openingId === attachment.id) return;
    setOpeningId(attachment.id);
    try {
      const url = await getAttachmentDownloadUrl(ticketId, attachment.id);
      window.open(url, "_blank", "noopener,noreferrer");
    } catch (err) {
      const msg = axios.isAxiosError(err)
        ? ((err.response?.data?.message as string | undefined) ?? "Không thể mở file.")
        : "Không thể mở file.";
      toast.error(msg);
    } finally {
      setOpeningId(null);
    }
  };

  const handleDelete = async (attachment: TicketAttachment) => {
    if (!window.confirm(`Xóa file "${attachment.originalFileName}"?`)) return;
    setDeletingId(attachment.id);
    try {
      await deleteMutation.mutateAsync(attachment.id);
      toast.success("Đã xóa file đính kèm.");
    } catch (err) {
      const msg = axios.isAxiosError(err)
        ? ((err.response?.data?.message as string | undefined) ?? "Không thể xóa file.")
        : "Không thể xóa file.";
      toast.error(msg);
    } finally {
      setDeletingId(null);
    }
  };

  if (attachments.length === 0) {
    return (
      <p className="text-xs text-muted-foreground text-center py-3 italic">
        Chưa có file đính kèm nào.
      </p>
    );
  }

  return (
    <ul className="space-y-2">
      {attachments.map((att) => {
        const isOpening = openingId === att.id;
        const isDeleting = deletingId === att.id;
        const canDeleteThis = canDelete && att.uploadedByUserId === currentUserId;

        return (
          <li
            key={att.id}
            className="flex items-center justify-between gap-3 p-2.5 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
          >
            <div className="flex items-center gap-2.5 min-w-0 flex-1">
              <div className="p-1.5 rounded-md bg-muted shrink-0">{getFileIcon(att.fileType)}</div>
              <div className="min-w-0 flex-1">
                <button
                  type="button"
                  onClick={() => void handleOpen(att)}
                  disabled={isOpening}
                  className="text-xs font-medium text-foreground hover:text-primary transition-colors truncate block text-left w-full"
                >
                  {att.originalFileName}
                </button>
                <div className="flex items-center gap-1.5 text-[10px] text-muted-foreground mt-0.5">
                  <span>{formatBytes(att.fileSize)}</span>
                  <span>·</span>
                  <span>{att.uploadedByUserName}</span>
                  <span>·</span>
                  <span>{formatDate(att.createdAt)}</span>
                </div>
              </div>
            </div>

            <div className="flex items-center gap-1 shrink-0">
              <Button
                variant="ghost"
                size="icon"
                className="h-7 w-7 text-muted-foreground hover:text-primary"
                onClick={() => void handleOpen(att)}
                disabled={isOpening}
                title="Tải về/Xem file"
              >
                {isOpening ? (
                  <Loader2 className="h-3.5 w-3.5 animate-spin" />
                ) : (
                  <Download className="h-3.5 w-3.5" />
                )}
              </Button>

              {canDeleteThis && (
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 text-muted-foreground hover:text-destructive"
                  onClick={() => void handleDelete(att)}
                  disabled={isDeleting}
                  title="Xóa file"
                >
                  {isDeleting ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <Trash2 className="h-3.5 w-3.5" />
                  )}
                </Button>
              )}
            </div>
          </li>
        );
      })}
    </ul>
  );
}
