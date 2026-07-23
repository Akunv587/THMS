// ===== Toast =====
function showToast(m, t, d) {
    var c = document.querySelector('.toast-container');
    if (!c) { c = document.createElement('div'); c.className = 'toast-container'; document.body.appendChild(c); }
    var icon = t === 'success' ? 'bi-check-circle-fill' : t === 'error' ? 'bi-x-circle-fill' : 'bi-exclamation-triangle-fill';
    var el = document.createElement('div');
    el.className = 'toast-item ' + t;
    el.innerHTML = '<i class="bi ' + icon + '"></i><span>' + escapeHtml(m) + '</span>';
    c.appendChild(el);
    setTimeout(function () { el.style.animation = 'toastOut 0.3s ease forwards'; setTimeout(function () { el.remove(); }, 300); }, d);
}
function showSuccess(m) { showToast(m, 'success', 3000); }
function showError(m) { showToast(m, 'error', 5000); }
function showWarning(m) { showToast(m, 'warning', 3000); }

// ===== HTML 转义 =====
function escapeHtml(s) { if (!s) return ''; return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;'); }

// ===== 防伪令牌 =====
function getAntiForgeryToken() { var t = document.querySelector('input[name="__RequestVerificationToken"]'); return t ? t.value : ''; }

// ===== 全局 Loading =====
var _loadingStack = 0;
function showLoading(t) {
    _loadingStack++;
    var el = document.getElementById('globalLoading');
    if (!el) {
        el = document.createElement('div');
        el.id = 'globalLoading';
        el.className = 'global-loading';
        el.innerHTML = '<div class="spinner-border" role="status"></div><div class="loading-text">' + (t || '加载中...') + '</div>';
        document.body.appendChild(el);
    }
    el.classList.add('show');
}
function hideLoading() { _loadingStack = Math.max(0, _loadingStack - 1); if (_loadingStack <= 0) { var el = document.getElementById('globalLoading'); if (el) el.classList.remove('show'); _loadingStack = 0; } }

// ===== 按钮 Loading =====
function btnLoading(btn, t) { btn.disabled = true; btn._orig = btn.innerHTML; btn.classList.add('btn-loading'); btn.innerHTML = t || btn.getAttribute('data-loading') || '处理中...'; }
function btnReset(btn) { btn.disabled = false; btn.classList.remove('btn-loading'); if (btn._orig) btn.innerHTML = btn._orig; }

// ===== Ajax 封装 =====
function ajaxPost(url, data, ok, fail) {
    var btn = document.activeElement;
    if (btn && btn.tagName === 'BUTTON') btnLoading(btn);
    $.ajax({
        url: url, type: 'POST', contentType: 'application/json', data: JSON.stringify(data),
        headers: { 'RequestVerificationToken': getAntiForgeryToken() },
        success: function (r) {
            if (btn && btn.tagName === 'BUTTON') btnReset(btn);
            if (r.success) { if (ok) ok(r); } else {
                var em = r.message || '操作失败';
                if (r.errors) { var a = []; for (var k in r.errors) a.push(r.errors[k]); em = a.join('<br>'); }
                showError(em); if (fail) fail(r);
            }
        },
        error: function (x) {
            if (btn && btn.tagName === 'BUTTON') btnReset(btn);
            var em = '系统繁忙，请稍后重试';
            try { var r = JSON.parse(x.responseText); em = r.message || em; } catch (e) {}
            if (x.status === 401) { window.location.href = '/Account/Login'; return; }
            if (x.status === 403) em = '没有操作权限';
            showError(em); if (fail) fail(x);
        }
    });
}

function ajaxGet(url, ok, fail) {
    $.ajax({
        url: url, type: 'GET',
        success: function (r) { if (r.success) { if (ok) ok(r); } else { showError(r.message || '操作失败'); if (fail) fail(r); } },
        error: function (x) {
            var em = '系统繁忙，请稍后重试';
            try { em = JSON.parse(x.responseText).message || em; } catch (e) {}
            if (x.status === 401) { window.location.href = '/Account/Login'; return; }
            showError(em); if (fail) fail(x);
        }
    });
}

// ===== 确认对话框 =====
function showConfirm(opts) {
    var d = $.extend({ title: '确认操作', content: '确定执行此操作？', confirmText: '确定', cancelText: '取消', danger: false }, opts);
    var id = 'confirmModalGlobal', exist = document.getElementById(id);
    if (exist) exist.remove();
    var html =
        '<div class="modal fade confirm-modal" id="' + id + '" tabindex="-1">'
        + '<div class="modal-dialog modal-sm modal-dialog-centered"><div class="modal-content">'
        + '<div class="modal-header"><h6 class="modal-title">' + escapeHtml(d.title) + '</h6>'
        + '<button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>'
        + '<div class="modal-body text-center">'
        + '<i class="bi ' + (d.danger ? 'bi-exclamation-triangle-fill text-warning' : 'bi-question-circle-fill text-primary') + '"></i>'
        + '<p class="mt-2 mb-0">' + escapeHtml(d.content) + '</p></div>'
        + '<div class="modal-footer justify-content-center gap-2">'
        + '<button type="button" class="btn btn-light" data-bs-dismiss="modal">' + escapeHtml(d.cancelText) + '</button>'
        + '<button type="button" class="btn ' + (d.danger ? 'btn-danger' : 'btn-primary') + '" id="confirmModalBtn">' + escapeHtml(d.confirmText) + '</button>'
        + '</div></div></div></div>';
    document.body.insertAdjacentHTML('beforeend', html);
    var modal = new bootstrap.Modal(document.getElementById(id));
    document.getElementById(id).addEventListener('hidden.bs.modal', function () { this.remove(); });
    document.getElementById('confirmModalBtn').addEventListener('click', function () { modal.hide(); if (d.onConfirm) d.onConfirm(); });
    modal.show();
}

// ===== 表格排序 =====
document.addEventListener('click', function (e) {
    var th = e.target.closest('th.sortable');
    if (!th) return;
    var table = th.closest('table');
    var colIdx = Array.from(th.parentElement.children).indexOf(th);
    var tbody = table.querySelector('tbody');
    if (!tbody) return;
    var rows = Array.from(tbody.querySelectorAll('tr:not(.empty-row)'));
    if (rows.length < 2) return;
    var isAsc = th.classList.contains('asc');
    // Reset all headers
    table.querySelectorAll('th.sortable').forEach(function (h) { h.classList.remove('asc', 'desc'); });
    th.classList.toggle('asc', !isAsc);
    th.classList.toggle('desc', isAsc);
    rows.sort(function (a, b) {
        var va = (a.children[colIdx]?.textContent || '').trim();
        var vb = (b.children[colIdx]?.textContent || '').trim();
        var na = parseFloat(va.replace(/[,￥¥$%]/g, ''));
        var nb = parseFloat(vb.replace(/[,￥¥$%]/g, ''));
        if (!isNaN(na) && !isNaN(nb)) return isAsc ? nb - na : na - nb;
        return isAsc ? vb.localeCompare(va, 'zh-CN') : va.localeCompare(vb, 'zh-CN');
    });
    rows.forEach(function (r) { tbody.appendChild(r); });
});

// ===== Flatpickr 日期选择器初始化 =====
if (typeof flatpickr !== 'undefined') {
    document.addEventListener('DOMContentLoaded', function () {
        // 日期输入框
        document.querySelectorAll('input[data-datepicker]').forEach(function (el) {
            flatpickr(el, { dateFormat: 'Y-m-d', allowInput: true });
        });
        // 日期范围
        document.querySelectorAll('input[data-daterange]').forEach(function (el) {
            flatpickr(el, { dateFormat: 'Y-m-d', mode: 'range', allowInput: true });
        });
        // datetime-local 改为 flatpickr
        document.querySelectorAll('input[type="datetime-local"]').forEach(function (el) {
            var cfg = { enableTime: true, dateFormat: 'Y-m-d H:i', allowInput: true, time_24hr: true };
            if (el.classList.contains('date-start')) cfg.defaultHour = 0;
            if (el.classList.contains('date-end')) cfg.defaultHour = 23;
            flatpickr(el, cfg);
        });
    });
}

// ===== 侧边栏 =====
$(document).on('click', '.menu-link.has-children', function (e) {
    e.preventDefault();
    var $s = $(this).siblings('.submenu');
    $s.slideToggle(200);
    $(this).toggleClass('expanded');
});

$(function () {
    var path = window.location.pathname.toLowerCase();
    $('.sidebar-menu .menu-link').each(function () {
        var href = ($(this).attr('href') || '').toLowerCase();
        if (href && path.startsWith(href) && href !== '#') {
            $(this).addClass('active');
            $(this).parents('.submenu').addClass('show');
            $(this).parents('.submenu').siblings('.has-children').addClass('expanded');
        }
    });
});

$(document).on('click', '.menu-toggle', function () { $('.sidebar').toggleClass('open'); });

// ===== 侧边栏遮罩层（移动端） =====
$(document).on('click', function (e) {
    if (window.innerWidth <= 1279 && $('.sidebar').hasClass('open')
        && !$(e.target).closest('.sidebar').length && !$(e.target).closest('.menu-toggle').length) {
        $('.sidebar').removeClass('open');
    }
});

// ===== 通知系统 =====
(function () {
    function updateBadge(c) { var b = $('#notificationBadge'); if (c > 0) { b.text(c > 99 ? '99+' : c).show(); } else { b.hide(); } }
    function loadDropdown() {
        $.ajax({ url: '/Notification/GetRecentUnread', type: 'GET',
            success: function (r) {
                if (!r.success) return;
                var items = r.data, $l = $('#notificationDropdownList');
                if (!items || !items.length) { $l.html('<div class="text-center py-4 text-muted small">暂无未读通知</div>'); return; }
                var ti = { 1: 'bi-check-circle', 2: 'bi-exclamation-triangle', 3: 'bi-tools', 4: 'bi-gear' };
                var tc = { 1: '#2563EB', 2: '#F59E0B', 3: '#0891B2', 4: '#64748b' };
                var tb = { 1: '#dbeafe', 2: '#fef3c7', 3: '#cffafe', 4: '#f1f5f9' };
                var h = '';
                items.forEach(function (n) {
                    h += '<a class="dropdown-item-notification" href="/Notification/Details/' + n.id + '">'
                        + '<div class="mini-icon" style="background:' + (tb[n.notificationType] || '#f1f5f9') + ';color:' + (tc[n.notificationType] || '#64748b') + ';"><i class="bi ' + (ti[n.notificationType] || 'bi-bell') + '"></i></div>'
                        + '<div class="mini-body"><div class="mini-title">' + escapeHtml(n.title) + '</div>'
                        + '<div class="mini-content">' + escapeHtml(n.content || '') + '</div>'
                        + '<div class="mini-time">' + (n.createTime ? n.createTime.substring(0, 16) : '') + '</div></div></a>';
                });
                $l.html(h);
            }
        });
    }
    function poll() { $.ajax({ url: '/Notification/GetUnreadCount', type: 'GET', success: function (r) { if (r.success) updateBadge(r.data); } }); }
    $(document).on('show.bs.dropdown', '.notification-btn', function () { loadDropdown(); });
    window.markAllNotificationsRead = function () {
        $.ajax({ url: '/Notification/MarkAllRead', type: 'POST', headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            success: function (r) { if (r.success) { updateBadge(0); $('#notificationDropdownList').html('<div class="text-center py-4 text-muted small">暂无未读通知</div>'); } }
        });
    };
    poll(); setInterval(poll, 60000);
})();

// ===== Enter 键快速搜索 =====
$(document).on('keydown', '.search-bar input', function (e) { if (e.key === 'Enter') { var btn = this.closest('.search-bar').querySelector('button[onclick^="doSearch"]'); if (btn) btn.click(); } });
