import Swal from 'sweetalert2';

// Mesmo visual dos alertas do pricing-webapp (Swal.fire com ícone e título).

export const alertSuccess = (text: string) =>
  Swal.fire({ icon: 'success', title: 'Sucesso!', text, showConfirmButton: false, timer: 2000 });

export const alertError = (text: string) => Swal.fire({ icon: 'error', title: 'Oops...', text });

// Os itens podem conter dados digitados por usuários (ex.: nome de grade), por
// isso são escapados antes de montar o HTML.
export const alertWarningList = (title: string, itens: string[], text?: string) =>
  Swal.fire({
    icon: 'warning',
    title,
    html:
      (text ? `<p>${escapeHtml(text)}</p>` : '') +
      `<ul style="text-align:left">${itens.map((item) => `<li>${escapeHtml(item)}</li>`).join('')}</ul>`,
  });

export const confirmAction = async (title: string, text: string, confirmButtonText: string): Promise<boolean> => {
  const result = await Swal.fire({
    icon: 'warning',
    title,
    text,
    showCancelButton: true,
    confirmButtonText,
    cancelButtonText: 'Cancelar',
    reverseButtons: true,
  });
  return result.isConfirmed;
};

const escapeHtml = (valor: string) =>
  valor.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
