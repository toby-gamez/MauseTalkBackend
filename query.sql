SELECT c.Name, c.Description, c.AllowInvites, cu.Role, u.Username FROM Chats c JOIN ChatUsers cu ON c.Id = cu.ChatId JOIN Users u ON cu.UserId = u.Id ORDER BY c.Name, cu.Role;
