$(document).ready(function() {
    console.log('Document ready - jQuery version:', $.fn.jquery);
    console.log('Bootstrap available:', typeof bootstrap !== 'undefined');
    
    // Helper function to get CSRF token
    window.getCsrfToken = function() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || 
               $('meta[name="csrf-token"]').attr('content') ||
               '';
    };
    
    // Global modal cleanup function to fix backdrop issues
    window.cleanupModalBackdrop = function() {
        // Remove all modal backdrops
        document.querySelectorAll('.modal-backdrop').forEach(backdrop => backdrop.remove());
        
        // Remove modal-open class from body
        document.body.classList.remove('modal-open');
        
        // Reset body styles that Bootstrap may have added
        document.body.style.removeProperty('padding-right');
        document.body.style.removeProperty('overflow');
        
        // Reset any modal states
        document.querySelectorAll('.modal').forEach(modal => {
            modal.setAttribute('aria-hidden', 'true');
            modal.style.removeProperty('display');
            modal.classList.remove('show');
        });
        
        console.log('Modal backdrop cleanup completed');
    };
    
    // Add global click handler to cleanup any stuck backdrops when clicking outside modals
    document.addEventListener('click', function(e) {
        if (e.target.classList.contains('modal-backdrop')) {
            setTimeout(cleanupModalBackdrop, 100);
        }
    });
    
    // All initialization code consolidated here
    
    // Make the next task card clickable on mobile
    const nextTaskCard = document.getElementById('next-task-card');
    if (nextTaskCard) {
        nextTaskCard.addEventListener('click', function(e) {
            // Don't trigger if clicking on the "View All" or "Start Working" buttons
            if (!e.target.closest('a')) {
                const startButton = this.querySelector('.btn-primary');
                if (startButton && startButton.href) {
                    window.location.href = startButton.href;
                }
            }
        });
    }

    // Initialize message inbox
    console.log('Initializing message inbox...');
    initializeMessageInbox();
    
    // Refresh inbox button
    if (document.getElementById('refreshInbox')) {
        document.getElementById('refreshInbox').addEventListener('click', function() {
            window.location.reload();
        });
    }
    
    // Setup search functionality
    var select = document.getElementById('searchType');
    var input = document.getElementById('indexSearchInput');
    var label = document.getElementById('indexSearchLabel');

    if (select && input && label) {
        function updateInputNameAndPlaceholder() {
            input.name = select.value;
            switch (select.value) {
                case "peNumber":
                    input.placeholder = "Enter PE Number";
                    label.textContent = "PE Number:";
                    break;
                case "customer":
                    input.placeholder = "Enter Customer Name";
                    label.textContent = "Customer:";
                    break;
                case "jobReference":
                    input.placeholder = "Enter Job Reference";
                    label.textContent = "Job Reference:";
                    break;
                case "soNumber":
                    input.placeholder = "Enter SO Number";
                    label.textContent = "SO Number:";
                    break;
            }
        }
        select.addEventListener('change', updateInputNameAndPlaceholder);
        updateInputNameAndPlaceholder();
    }

    // Handle urgent request buttons
    $(document).on('click', '#acceptUrgentRequestBtn', function() {
        const requestType = $('#urgentRequestType').val();
        const requestId = $('#urgentRequestId').val();
        let priorityText = '';
        // Try to find the row with label 'Priority:'
        $('#urgentRequestDetails .row').each(function() {
            const label = $(this).find('.col-sm-4').text().trim().toLowerCase();
            if (label.startsWith('priority')) {
                priorityText = $(this).find('.col-sm-8').text().trim();
            }
        });

        // Normalize the text for robust matching
        const normPriority = priorityText.replace(/\s+/g, '').toLowerCase();
        let urgentReason = 'OpeningCeremony'; // Default fallback
        if (normPriority.includes('priority2') || normPriority.includes('criticalcustomer')) {
            urgentReason = 'CriticalCustomer';
        } else if (normPriority.includes('priority1') || normPriority.includes('openingceremony')) {
            urgentReason = 'OpeningCeremony';
        }
        console.log('Priority text:', priorityText, '| Normalized:', normPriority, '| Urgent reason:', urgentReason);

        const form = $('#urgentRequestForm');
        // Remove any previous urgentReason fields
        form.find('input[name="urgentReason"]').remove();
        $('<input>').attr({type: 'hidden', name: 'urgentReason', value: urgentReason}).appendTo(form);
        if (requestType === 'pe') {
            form.attr('action', APP_BASE + 'PlannedEvents/ProcessUrgentRequest');
        } else {
            form.attr('action', APP_BASE + 'PETasks/ProcessUrgentRequest');
        }
        form.submit();
    });

    $(document).on('click', '#rejectUrgentRequestBtn', function() {
        const requestType = $('#urgentRequestType').val();
        const requestId = $('#urgentRequestId').val();
        const form = $('#urgentRequestForm');
        // Remove any previous urgentReason fields
        form.find('input[name="urgentReason"]').remove();
        $('<input>').attr({type: 'hidden', name: 'urgentReason', value: 'Reject'}).appendTo(form);
        if (requestType === 'pe') {
            form.attr('action', APP_BASE + 'PlannedEvents/ProcessUrgentRequest');
        } else {
            form.attr('action', APP_BASE + 'PETasks/ProcessUrgentRequest');
        }
        form.submit();
    });
    
    // Issue resolution handlers
    $('#replyIssueBtn').on('click', function() {
        const issueId = $(this).data('issue-id');
        const peId = $(this).data('pe-id');
        const senderId = $(this).data('sender-id');
        const originalIssueId = $(this).data('original-issue-id') || issueId;
        
        // Set values for the reply form
        $('#replyIssueId').val(originalIssueId);
        $('#replyPlannedEventId').val(peId);
        $('#replyReceiverId').val(senderId);
        
        // Hide the details modal and show the reply modal with proper accessibility
        $('#issueDetailsModal').modal('hide');
        
        const replyModalElement = document.getElementById('issueReplyModal');
        const replyModal = new bootstrap.Modal(replyModalElement);
        
        // Fix accessibility issue
        replyModalElement.addEventListener('shown.bs.modal', function() {
            replyModalElement.removeAttribute('aria-hidden');
            const firstFocusable = replyModalElement.querySelector('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])');
            if (firstFocusable) {
                firstFocusable.focus();
            }
        });
        
        replyModalElement.addEventListener('hidden.bs.modal', function() {
            replyModalElement.setAttribute('aria-hidden', 'true');
            
            // Use global cleanup function with a small delay
            setTimeout(cleanupModalBackdrop, 50);
        });
        
        replyModal.show();
    });
    
    $('#resolveIssueBtn').on('click', function() {
        const issueId = $(this).data('issue-id');
        const peId = $(this).data('pe-id');
        
        // Set values for the resolve form
        $('#resolveIssueId').val(issueId);
        $('#resolvePlannedEventId').val(peId);
        
        // Hide the details modal and show the resolve modal with proper accessibility
        $('#issueDetailsModal').modal('hide');
        
        const resolveModalElement = document.getElementById('issueResolveModal');
        const resolveModal = new bootstrap.Modal(resolveModalElement);
        
        // Fix accessibility issue
        resolveModalElement.addEventListener('shown.bs.modal', function() {
            resolveModalElement.removeAttribute('aria-hidden');
            const firstFocusable = resolveModalElement.querySelector('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])');
            if (firstFocusable) {
                firstFocusable.focus();
            }
        });
        
        resolveModalElement.addEventListener('hidden.bs.modal', function() {
            resolveModalElement.setAttribute('aria-hidden', 'true');
            
            // Use global cleanup function with a small delay
            setTimeout(cleanupModalBackdrop, 50);
        });
        
        resolveModal.show();
    });
    
    $('#confirmResolutionBtn').on('click', function() {
        const form = $('#confirmResolutionForm');
        
        // First make sure the input isn't duplicated if clicked multiple times
        form.find('input[name="isConfirmed"]').remove();
        
        // Add the isConfirmed field
        $('<input>').attr({
            type: 'hidden',
            name: 'isConfirmed',
            value: 'true'
        }).appendTo(form);
        
        // Actually submit the form
        form.submit();
        
        // Log the submission for debugging
        console.log('Submitting confirmation form with resolution ID:', $('#resolutionId').val());
    });
    
    $('#rejectResolutionBtn').on('click', function() {
        const form = $('#confirmResolutionForm');
        $('<input>').attr({
            type: 'hidden',
            name: 'isConfirmed',
            value: 'false'
        }).appendTo(form);
        form.submit();
    });
});

// Functions defined outside document ready to avoid scoping issues
function initializeMessageInbox() {
    console.log('Initializing message inbox...');
    
    const messageItems = document.querySelectorAll('.message-item');
    const filterButtons = document.querySelectorAll('.inbox-filters button');
    const inboxSearch = document.querySelector('.inbox-search');
    
    console.log('Found message items:', messageItems.length);
    console.log('Found filter buttons:', filterButtons.length);
    console.log('Found inbox search:', !!inboxSearch);
    
    // Debug: log all message items
    messageItems.forEach((item, index) => {
        console.log(`Message ${index}:`, {
            classes: item.className,
            isUnread: item.classList.contains('unread'),
            onclick: item.onclick,
            hasDataAttrs: !!item.dataset.issueId
        });
    });
    
    // Check which filter button is initially active and apply the appropriate filter
    const activeFilter = document.querySelector('.inbox-filters button.active');
    const initialFilter = activeFilter ? activeFilter.getAttribute('data-filter') : 'unread';
    
    console.log('Initial filter:', initialFilter);
    
    // Apply initial filter
    messageItems.forEach(item => {
        const isUnread = item.classList.contains('unread');
        
        switch(initialFilter) {
            case 'unread':
                item.style.display = isUnread ? 'flex' : 'none';
                break;
            case 'read':
                item.style.display = !isUnread ? 'flex' : 'none';
                break;
            case 'all':
                item.style.display = 'flex';
                break;
            default:
                item.style.display = 'flex'; // Show all by default if no filter is active
        }
    });

    // Message filtering
    filterButtons.forEach(button => {
        button.addEventListener('click', function() {
            console.log('Filter button clicked:', this.getAttribute('data-filter'));
            
            // Remove active class from all buttons
            filterButtons.forEach(btn => btn.classList.remove('active'));
            // Add active class to clicked button
            this.classList.add('active');

            const filter = this.getAttribute('data-filter');

            // Show/hide messages based on filter
            messageItems.forEach(item => {
                const isUnread = item.classList.contains('unread');
                
                switch(filter) {
                    case 'unread':
                        // Show only unread messages
                        item.style.display = isUnread ? 'flex' : 'none';
                        break;
                    case 'read':
                        // Show only read messages
                        item.style.display = !isUnread ? 'flex' : 'none';
                        break;
                    case 'all':
                        // Show all messages
                        item.style.display = 'flex';
                        break;
                }
            });
        });
    });
    
    // Handle search functionality
    if (inboxSearch) {
        inboxSearch.addEventListener('input', function() {
            const searchTerm = this.value.toLowerCase();
            
            messageItems.forEach(item => {
                const content = item.textContent.toLowerCase();
                if (content.includes(searchTerm)) {
                    item.style.display = 'flex';
                } else {
                    item.style.display = 'none';
                }
            });
        });
    }

    // Handle message clicks for urgent requests
    document.querySelectorAll('.urgent-request').forEach(item => {
        item.addEventListener('click', function() {
            // Extract the ID from data-message-id attribute
            const messageId = this.getAttribute('data-message-id');
            const peId = messageId.replace('urgent-pe-', '');
            
            // Show PE urgent confirmation modal with details
            showUrgentRequestModal('pe', peId);
        });
    });

    // Handle task urgent request clicks
    document.querySelectorAll('.task-urgent-request').forEach(item => {
        item.addEventListener('click', function() {
            // Extract the ID from data-message-id attribute
            const messageId = this.getAttribute('data-message-id');
            const taskId = messageId.replace('urgent-task-', '');
            
            // Show task urgent confirmation modal
            showUrgentRequestModal('task', taskId);
        });
    });

    // Handle resolution request message clicks (these need special handling)
    document.querySelectorAll('.message-item.resolution-request[data-message-id^="issue-"]').forEach(item => {
        item.addEventListener('click', function() {
            // Extract resolution request data from the element's data attributes
            const issueId = this.getAttribute('data-issue-id');
            const peId = this.getAttribute('data-pe-id');
            const resolutionId = this.getAttribute('data-resolution-id');
            const resolutionDetails = this.getAttribute('data-resolution-details');
            
            // Set up data attributes for the showResolutionConfirm function
            $(this).data('issue-id', issueId);
            $(this).data('pe-id', peId);
            $(this).data('resolution-id', resolutionId);
            $(this).data('resolution-details', resolutionDetails);
            
            // Show resolution confirmation modal directly
            showResolutionConfirm(this);
        });
    });

    // Handle regular issue message clicks (excluding resolution requests)
    document.querySelectorAll('.message-item[data-message-id^="issue-"]:not(.resolution-request)').forEach(item => {
        item.addEventListener('click', function() {
            // Extract issue data from the element's data attributes and classes
            const messageId = this.getAttribute('data-message-id');
            const issueId = this.getAttribute('data-issue-id');
            const peId = this.getAttribute('data-pe-id');
            
            // Get other data from the element's data attributes
            const sender = this.getAttribute('data-sender');
            const senderId = this.getAttribute('data-sender-id');
            const issueText = this.getAttribute('data-issue-text');
            const attachment = this.getAttribute('data-attachment');
            const date = this.getAttribute('data-date');
            const isRead = this.getAttribute('data-is-read') === 'true';
            const isResolutionRequest = this.getAttribute('data-is-resolution-request') === 'true';
            const originalIssueId = this.getAttribute('data-original-issue-id');
            
            // Set up data attributes for the showIssueDetails function
            $(this).data('issue-id', issueId);
            $(this).data('pe-id', peId);
            $(this).data('sender', sender);
            $(this).data('sender-id', senderId);
            $(this).data('issue-text', issueText);
            $(this).data('attachment', attachment);
            $(this).data('date', date);
            $(this).data('is-read', isRead);
            $(this).data('is-resolution-request', isResolutionRequest);
            $(this).data('original-issue-id', originalIssueId);
            
            // Show issue details modal
            showIssueDetails(this);
        });
    });
}

function showUrgentRequestModal(type, id) {
    // Set the form values
    document.getElementById('urgentRequestType').value = type;
    document.getElementById('urgentRequestId').value = id;
    
    // Fetch record details based on type (PE or Task)
    let url = '';
    if (type === 'pe') {
    url = `${APP_BASE}api/planned-events/${id}/basic-details`;
    } else {
    url = `${APP_BASE}PETasks/GetUrgentRequestDetails/${id}`; // Keep existing for tasks until task API is ready
    }
    
    // Show loading indicator
    document.getElementById('urgentRequestDetails').innerHTML = '<div class="text-center"><div class="spinner-border text-primary" role="status"></div><p class="mt-2">Loading details...</p></div>';
    
    // Show the modal with proper accessibility handling while fetching details
    const modalElement = document.getElementById('urgentRequestModal');
    const modal = new bootstrap.Modal(modalElement);
    
    // Fix accessibility issue by ensuring aria-hidden is properly managed
    modalElement.addEventListener('shown.bs.modal', function() {
        // Remove aria-hidden when modal is fully shown to prevent accessibility conflicts
        modalElement.removeAttribute('aria-hidden');
        
        // Focus on the first focusable element in the modal
        const firstFocusable = modalElement.querySelector('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])');
        if (firstFocusable) {
            firstFocusable.focus();
        }
    });
    
    modalElement.addEventListener('hidden.bs.modal', function() {
        // Restore aria-hidden when modal is hidden
        modalElement.setAttribute('aria-hidden', 'true');
        
        // Use global cleanup function with a small delay
        setTimeout(cleanupModalBackdrop, 50);
    });
    
    modal.show();
    
    // Fetch details from server
    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            // Update the modal with details
            let detailsHtml = `
        <div class="alert alert-info mb-3">
            <div class="row">
                <div class="col-sm-4 fw-bold">PE Number:</div>
                <div class="col-sm-8">${data.peNumber}</div>
            </div>
            <div class="row mt-2">
                <div class="col-sm-4 fw-bold">Customer:</div>
                <div class="col-sm-8">${data.customer || 'Not specified'}</div>
            </div>
            <div class="row mt-2">
                <div class="col-sm-4 fw-bold">Requested By:</div>
                <div class="col-sm-8">${data.urgentRequestedByName || 'Unknown'}</div>
            </div>`;
                    
            if (type === 'task') {
                detailsHtml += `
                    <div class="row mt-2">
                        <div class="col-sm-4 fw-bold">Task:</div>
                        <div class="col-sm-8">${data.taskName}</div>
                    </div>`;
            }
            
            detailsHtml += `
                <div class="row mt-2">
                    <div class="col-sm-4 fw-bold">Priority:</div>
                    <div class="col-sm-8">${data.priority || 'Not specified'}</div>
                </div>
            </div>`;
            
            // Show request reason if available
            if (data.urgentRequestReason) {
                detailsHtml += `
                <div class="alert alert-warning">
                    <h6 class="fw-bold">Request Reason:</h6>
                    <p>${data.urgentRequestReason}</p>
                </div>`;
            }
            
            document.getElementById('urgentRequestDetails').innerHTML = detailsHtml;
        })
        .catch(error => {
            console.error('Error fetching urgent request details:', error);
            
            // If it's a PE request, try the fallback controller endpoint
            if (type === 'pe') {
                fetch(`${APP_BASE}PlannedEvents/GetBasicDetails/${id}`)
                    .then(response => {
                        if (!response.ok) {
                            throw new Error('Fallback endpoint also failed');
                        }
                        return response.json();
                    })
                    .then(data => {
                        // Update the modal with details from controller fallback
                        let detailsHtml = `
                    <div class="alert alert-info mb-3">
                        <div class="row">
                            <div class="col-sm-4 fw-bold">PE Number:</div>
                            <div class="col-sm-8">${data.peNumber}</div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-sm-4 fw-bold">Customer:</div>
                            <div class="col-sm-8">${data.customer || 'Not specified'}</div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-sm-4 fw-bold">Status:</div>
                            <div class="col-sm-8"><span class="badge ${data.peStatus === 'Hold' ? 'bg-warning' : 'bg-primary'}">${data.peStatus}</span></div>
                        </div>
                    </div>`;
                        
                        document.getElementById('urgentRequestDetails').innerHTML = detailsHtml;
                    })
                    .catch(fallbackError => {
                        console.error('Both urgent request endpoints failed:', fallbackError);
                        document.getElementById('urgentRequestDetails').innerHTML = `
                            <div class="alert alert-danger">
                                <i class="fas fa-exclamation-circle me-2"></i>
                                Failed to load request details. Please try again.
                            </div>`;
                    });
            } else {
                // For task requests, show error directly since no fallback is needed
                document.getElementById('urgentRequestDetails').innerHTML = `
                    <div class="alert alert-danger">
                        <i class="fas fa-exclamation-circle me-2"></i>
                        Failed to load request details. Please try again.
                    </div>`;
            }
        });

         const viewDetailsBtn = document.getElementById('urgentRequestViewDetailsBtn');
         if (viewDetailsBtn) {
             if (type === 'pe') {
                 viewDetailsBtn.href = APP_BASE + 'PlannedEvents/Details/' + id;
             } else {
                 viewDetailsBtn.href = APP_BASE + 'PETasks/Details/' + id;
             }
         }
}

function showIssueDetails(element) {
    try {
        // Extract issue data from the element's data attributes
        const issueId = $(element).data('issue-id');
        const peId = $(element).data('pe-id');
        const sender = $(element).data('sender');
        const senderId = $(element).data('sender-id');
        const issueText = $(element).data('issue-text');
        const attachment = $(element).data('attachment');
        const date = $(element).data('date');
        const isRead = $(element).data('is-read') === 'true';
        const isResolutionRequest = $(element).data('is-resolution-request') === 'true';
        const originalIssueId = $(element).data('original-issue-id');
        
        console.log('Issue data:', {
            issueId, peId, sender, senderId, issueText, attachment, date, isRead, isResolutionRequest, originalIssueId
        });
        
        // Validate required data
        if (!issueId || !peId || !sender) {
            console.error('Missing required issue data:', { issueId, peId, sender });
            alert('Error: Missing issue data. Please refresh the page and try again.');
            return;
        }
        
        // Build the issue details content
        let detailsHtml = `
            <div class="message-detail-header">
                <h5>${isResolutionRequest ? '<span class="badge bg-success">Resolution Request</span> ' : ''}${issueText || 'No issue text available'}</h5>
                <div class="message-info">
                    <span class="fw-bold">From:</span> ${sender} · 
                    <span class="fw-bold">Date:</span> ${date || 'Unknown date'}
                </div>
            </div>
            <div class="message-detail-body my-3">
                <p>${issueText || 'No issue text available'}</p>
                ${attachment && attachment !== 'null' ? `<div class="attachment-section mt-3">
                    <h6 class="fw-bold">Attachment:</h6>
                    <a href="${attachment}" target="_blank" class="btn btn-sm btn-outline-primary">
                        <i class="fas fa-paperclip me-2"></i>View Attachment
                    </a>
                </div>` : ''}
            </div>`;
        
        // Update the modal content
        $('#issueDetailsContent').html(detailsHtml);
    $('#viewPEDetailsBtn').attr('href', `${APP_BASE}PlannedEvents/Details/${peId}`);
        
        // Set up the reply button data
        $('#replyIssueBtn').data('issue-id', issueId);
        $('#replyIssueBtn').data('pe-id', peId);
        $('#replyIssueBtn').data('sender-id', senderId);
        $('#replyIssueBtn').data('original-issue-id', originalIssueId);
        
        // Set up the resolve button data
        $('#resolveIssueBtn').data('issue-id', issueId);
        $('#resolveIssueBtn').data('pe-id', peId);
        
        // Handle resolution request specific UI
        if (isResolutionRequest) {
            $('#replyIssueBtn').addClass('d-none');
            $('#resolveIssueBtn').addClass('d-none');
            
            // Check if there's a resolution record for this issue
            $.get(`${APP_BASE}Issues/GetResolution/${originalIssueId || issueId}`, function(resolution) {
                if (resolution && resolution.id) {
                    // Show resolution confirmation section
                    $('#resolutionConfirmationSection').removeClass('d-none');
                    $('#resolutionId').val(resolution.id);
                }
            }).fail(function(xhr, status, error) {
                console.error('Error fetching resolution:', error);
            });
        } else {
            $('#replyIssueBtn').removeClass('d-none');
            $('#resolveIssueBtn').removeClass('d-none');
            $('#resolutionConfirmationSection').addClass('d-none');
        }
        
        // Fetch PE record details
        fetchPEDetails(peId);
        
        // Mark as read if not already read
        if (!isRead) {
            markIssueAsRead(issueId);
            $(element).removeClass('unread');
            $(element).data('is-read', 'true');
            
            // Update unread count in the header
            updateUnreadCount();
        }
        
        // Show the modal with proper accessibility handling
        const modalElement = document.getElementById('issueDetailsModal');
        if (!modalElement) {
            console.error('Issue details modal not found');
            alert('Error: Modal not found. Please refresh the page.');
            return;
        }
        
        const modal = new bootstrap.Modal(modalElement);
        
        // Fix accessibility issue by ensuring aria-hidden is properly managed
        modalElement.addEventListener('shown.bs.modal', function() {
            // Remove aria-hidden when modal is fully shown to prevent accessibility conflicts
            modalElement.removeAttribute('aria-hidden');
            
            // Focus on the first focusable element in the modal
            const firstFocusable = modalElement.querySelector('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])');
            if (firstFocusable) {
                firstFocusable.focus();
            }
        });
        
        modalElement.addEventListener('hidden.bs.modal', function() {
            // Restore aria-hidden when modal is hidden
            modalElement.setAttribute('aria-hidden', 'true');
            
            // Use global cleanup function with a small delay
            setTimeout(cleanupModalBackdrop, 50);
        });
        
        modal.show();
    } catch (error) {
        console.error('Error in showIssueDetails:', error);
        alert('Error displaying issue details. Please refresh the page and try again.');
    }
}

function showResolutionConfirm(element) {
    try {
        const resolutionId = $(element).data('resolution-id');
        const resolutionDetails = $(element).data('resolution-details');
        const peId = $(element).data('pe-id');
        const issueId = $(element).data('issue-id');
        
        console.log('Resolution data:', {
            resolutionId,
            resolutionDetails,
            peId,
            issueId
        });
        
        // Validate required data
        if (!peId || !issueId) {
            console.error('Missing required resolution data:', { peId, issueId });
            alert('Error: Missing resolution data. Please refresh the page and try again.');
            return;
        }
        
        // Always clear previous values and content
        $('#directResolutionId').val('');
        $('#resolutionDetailsText').html('<div class="spinner-border spinner-border-sm text-primary" role="status"></div><span class="ms-2">Loading resolution details...</span>');
        
        // Check if we have the resolution ID from the data attribute
        if (resolutionId && resolutionId !== '0') {
            $('#directResolutionId').val(resolutionId);
            
            // If we also have the details from the attribute, use them directly
            if (resolutionDetails && String(resolutionDetails).trim() !== '') {
                $('#resolutionDetailsText').text(String(resolutionDetails));
            } else {
                // Otherwise fetch the details for this resolution
                fetchResolutionDetails(resolutionId);
            }
        } else {
            // If no resolution ID from data attribute, query by issue ID
            fetchResolutionByIssueId(issueId);
        }
        
        // Set up other modal elements
    $('#viewPEDetailsLink').attr('href', `${APP_BASE}PlannedEvents/Details/${peId}`);
        
        // Show the modal with proper accessibility handling
        const modalElement = document.getElementById('resolutionConfirmModal');
        if (!modalElement) {
            console.error('Resolution confirm modal not found');
            alert('Error: Modal not found. Please refresh the page.');
            return;
        }
        
        const modal = new bootstrap.Modal(modalElement);
        
        // Fix accessibility issue by ensuring aria-hidden is properly managed
        modalElement.addEventListener('shown.bs.modal', function() {
            // Remove aria-hidden when modal is fully shown to prevent accessibility conflicts
            modalElement.removeAttribute('aria-hidden');
            
            // Focus on the first focusable element in the modal
            const firstFocusable = modalElement.querySelector('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])');
            if (firstFocusable) {
                firstFocusable.focus();
            }
        });
        
        modalElement.addEventListener('hidden.bs.modal', function() {
            // Restore aria-hidden when modal is hidden
            modalElement.setAttribute('aria-hidden', 'true');
            
            // Use global cleanup function with a small delay
            setTimeout(cleanupModalBackdrop, 50);
        });
        
        modal.show();
        
        // Mark as read
        if (issueId) {
            markIssueAsRead(issueId);
            $(element).removeClass('unread');
            $(element).data('is-read', 'true');
            updateUnreadCount();
        }
    } catch (error) {
        console.error('Error in showResolutionConfirm:', error);
        alert('Error displaying resolution confirmation. Please refresh the page and try again.');
    }
}

function fetchResolutionDetails(resolutionId) {
    fetch(`${APP_BASE}api/issues/resolution/${resolutionId}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(response => {
            // Handle new API response format
            const data = response.data || response;
            if (data && data.details) {
                $('#resolutionDetailsText').text(data.details);
            } else {
                $('#resolutionDetailsText').html('<em>No resolution details available</em>');
            }
        })
        .catch(err => {
            console.error('Error fetching resolution details:', err);
            $('#resolutionDetailsText').html('<em class="text-danger">Could not load resolution details. Please try again later.</em>');
        });
}

function fetchResolutionByIssueId(issueId) {
    fetch(`${APP_BASE}api/issues/${issueId}/resolution`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(response => {
            // Handle new API response format
            const data = response.data || response;
            if (data && data.id) {
                $('#directResolutionId').val(data.id);
                $('#resolutionDetailsText').text(data.details || 'No details provided');
            } else {
                $('#resolutionDetailsText').html('<em>No resolution found for this issue</em>');
                // Hide the buttons if no resolution is found
                $('#confirmResolutionForm button').hide();
            }
        })
        .catch(err => {
            console.error('Error fetching resolution:', err);
            $('#resolutionDetailsText').html('<em class="text-danger">Could not load resolution details. Please try again later.</em>');
        });
}

function submitResolutionForm(isConfirmed) {
    // Set the hidden field value based on the button clicked
    $('#directIsConfirmed').val(isConfirmed);
    
    // Get the resolution ID and validate it
    const resolutionId = $('#directResolutionId').val();
    console.log('Submitting form with Resolution ID:', resolutionId);
    console.log('Is Confirmed:', isConfirmed);
    
    if (!resolutionId || resolutionId === '0') {
        alert('Error: No resolution ID found. Please refresh the page and try again.');
        return;
    }
    
    // Disable buttons to prevent double submission
    const buttons = $('#confirmResolutionForm button');
    buttons.prop('disabled', true);
    
    // Add a spinner to the button that was clicked
    const clickedButton = isConfirmed ? 
        $('#confirmResolutionForm button:contains("Yes")') : 
        $('#confirmResolutionForm button:contains("No")');
    
    const originalButtonHtml = clickedButton.html();
    clickedButton.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...');
    
    // Submit the form
    try {
        document.getElementById('confirmResolutionForm').submit();
    } catch (err) {
        console.error('Error submitting form:', err);
        alert('Error submitting form. Please try again.');
        
        // Re-enable buttons on error
        buttons.prop('disabled', false);
        clickedButton.html(originalButtonHtml);
    }
}

function fetchPEDetails(peId) {
    // Try the new API endpoint first
    fetch(`${APP_BASE}api/planned-events/${peId}/basic-details`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(response => {
            // Handle the new API response format
            const data = response.data || response;
            
            // Update the PE details section with basic info
            let peDetailsHtml = `
                <div class="row">
                    <div class="col-md-6">
                        <div class="mb-2"><strong>PE Number:</strong> ${data.peNumber}</div>
                        <div class="mb-2"><strong>Customer:</strong> ${data.customer || 'N/A'}</div>
                        <div class="mb-2"><strong>Status:</strong> <span class="badge ${data.peStatus === 'Hold' ? 'bg-warning' : 'bg-primary'}">${data.peStatus}</span></div>
                    </div>
                    <div class="col-md-6">
                        <div class="mb-2"><strong>Service Type:</strong> ${data.serviceType || 'N/A'}</div>
                        <div class="mb-2"><strong>Task:</strong> ${data.taskName}</div>
                        <div class="mb-2"><strong>Workgroup:</strong> ${data.taskWorkGroup || data.taskWg}</div>
                    </div>
                </div>`;
            
            $('#relatedPEDetails').html(peDetailsHtml);
        })
        .catch(error => {
            console.error('API endpoint failed, trying fallback controller endpoint:', error);
            
            // Fallback to controller endpoint
            fetch(`${APP_BASE}PlannedEvents/GetBasicDetails/${peId}`)
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Controller endpoint also failed');
                    }
                    return response.json();
                })
                .then(data => {
                    // Update the PE details section with basic info from controller
                    let peDetailsHtml = `
                        <div class="row">
                            <div class="col-md-6">
                                <div class="mb-2"><strong>PE Number:</strong> ${data.peNumber}</div>
                                <div class="mb-2"><strong>Customer:</strong> ${data.customer || 'N/A'}</div>
                                <div class="mb-2"><strong>Status:</strong> <span class="badge ${data.peStatus === 'Hold' ? 'bg-warning' : 'bg-primary'}">${data.peStatus}</span></div>
                            </div>
                            <div class="col-md-6">
                                <div class="mb-2"><strong>Service Type:</strong> ${data.serviceType || 'N/A'}</div>
                                <div class="mb-2"><strong>Task:</strong> ${data.taskName}</div>
                                <div class="mb-2"><strong>Workgroup:</strong> ${data.taskWorkGroup || data.taskWg}</div>
                            </div>
                        </div>`;
                    
                    $('#relatedPEDetails').html(peDetailsHtml);
                })
                .catch(fallbackError => {
                    console.error('Both API and controller endpoints failed:', fallbackError);
                    $('#relatedPEDetails').html(`
                        <div class="alert alert-danger">
                            <i class="fas fa-exclamation-circle me-2"></i>
                            Failed to load PE details. Please try again.
                        </div>`);
                });
        });
}

function updateUnreadCount() {
    // Count actual unread messages in the DOM
    const unreadMessages = document.querySelectorAll('.message-item.unread');
    const unreadCount = unreadMessages.length;
    
    const inboxHeader = document.querySelector('.inbox-header h6');
    if (inboxHeader) {
        // Remove existing badge
        const existingBadge = inboxHeader.querySelector('.badge');
        if (existingBadge) {
            existingBadge.remove();
        }
        
        // Add new badge if there are unread messages
        if (unreadCount > 0) {
            const badge = document.createElement('span');
            badge.className = 'badge bg-warning text-dark ms-2';
            badge.textContent = `${unreadCount} Unread`;
            inboxHeader.appendChild(badge);
        }
    }
    
    console.log(`Unread count updated: ${unreadCount}`);
}

function markIssueAsRead(issueId) {
    // Call new secure API to mark issue as read
    fetch(`${APP_BASE}api/issues/${issueId}/mark-read`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getCsrfToken()
        }
    }).then(response => {
        if (!response.ok) {
            throw new Error('API endpoint failed');
        }
        return response.json();
    }).then(data => {
        if (data && !data.success) {
            console.error('API error marking issue as read:', data.message);
            throw new Error('API returned error');
        } else {
            // Successfully marked as read - update UI
            updateMessageUIAsRead(issueId);
        }
    }).catch(error => {
        console.error('API failed, trying fallback controller endpoint:', error);
        
        // Fallback to controller endpoint
    fetch(`${APP_BASE}Issues/MarkAsRead/${issueId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getCsrfToken()
            }
        }).then(response => {
            if (!response.ok) {
                throw new Error('Controller endpoint also failed');
            }
            return response.json();
        }).then(data => {
            if (data && data.success) {
                // Successfully marked as read via fallback - update UI
                updateMessageUIAsRead(issueId);
            } else {
                console.error('Controller endpoint error:', data?.message || 'Unknown error');
            }
        }).catch(fallbackError => {
            console.error('Both API and controller endpoints failed:', fallbackError);
            // Still update UI optimistically
            updateMessageUIAsRead(issueId);
        });
    });
}

function updateMessageUIAsRead(issueId) {
    // Find the message element for this issue
    const messageElement = document.querySelector(`[data-issue-id="${issueId}"]`);
    if (messageElement) {
        // Remove unread class to mark as read
        messageElement.classList.remove('unread');
        
        // Update the data attribute
        messageElement.setAttribute('data-is-read', 'true');
        
        // Update the message content if needed (remove unread indicators)
        const unreadIndicators = messageElement.querySelectorAll('.unread-indicator, .badge-warning');
        unreadIndicators.forEach(indicator => {
            indicator.remove();
        });
        
        // If currently viewing "unread" filter, hide this message
        const activeFilter = document.querySelector('.inbox-filters .btn.active');
        if (activeFilter && activeFilter.getAttribute('data-filter') === 'unread') {
            messageElement.style.display = 'none';
        }
        
        // Update the unread count
        updateUnreadCount();
        
        console.log(`Message ${issueId} marked as read in UI`);
    }
}

// Debug function to test issue modal functionality
window.testIssueModal = function() {
    console.log('Testing issue modal...');
    const testElement = {
        dataset: {
            issueId: '123',
            peId: '456',
            sender: 'Test User',
            senderId: '789',
            issueText: 'Test issue text',
            date: 'Jan 01, 2024 10:00',
            isRead: 'false',
            isResolutionRequest: 'false'
        }
    };
    
    // Convert to jQuery-like data access
    const $testElement = {
        data: function(key) {
            const dataKey = key.replace(/-([a-z])/g, function(match, letter) {
                return letter.toUpperCase();
            });
            return testElement.dataset[dataKey];
        }
    };
    
    showIssueDetails($testElement);
};

// Debug function to check message items
window.debugMessages = function() {
    console.log('=== MESSAGE DEBUG ===');
    const messageItems = document.querySelectorAll('.message-item');
    console.log('Total message items found:', messageItems.length);
    
    messageItems.forEach((item, index) => {
        console.log(`Message ${index}:`, {
            element: item,
            classes: item.className,
            isUnread: item.classList.contains('unread'),
            style: item.style.display,
            onclick: item.onclick,
            issueId: item.dataset.issueId,
            peId: item.dataset.peId,
            sender: item.dataset.sender,
            allDataAttrs: Object.keys(item.dataset)
        });
    });
    
    console.log('=== FILTER BUTTONS ===');
    const filterButtons = document.querySelectorAll('.inbox-filters button');
    filterButtons.forEach((btn, index) => {
        console.log(`Filter ${index}:`, {
            text: btn.textContent,
            filter: btn.getAttribute('data-filter'),
            isActive: btn.classList.contains('active')
        });
    });
};